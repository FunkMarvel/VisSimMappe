// //////////////////////////////////////////////////////////////////////////
// //////////////////////////////
// //FileName: PointCloudSurfaceParser.cpp
// //FileType: Visual C++ Source file
// //Author : Anders P. Åsbø
// //Created On : 01/10/2023
// //Last Modified On : 18/11/2023
// //Copy Rights : Anders P. Åsbø
// //Description : Program for creating regular triangulated surface from point cloud.
// //////////////////////////////////////////////////////////////////////////
// //////////////////////////////

#include <fstream>
#include <iostream>
#include <string>
#include <vector>
#include <eigen3/Eigen/Eigen>

struct DataBounds;
DataBounds readData(std::ifstream& file, std::vector<Eigen::Vector3f>& dataContainer);
void writeVertexData(const std::string& filePath, const std::vector<std::vector<Eigen::Vector3f>>& dataGrid,
                     float scaleX, float scaleY, float scaleZ);
void writeIndexFile(const std::string& filePath, int numX, int numY);

/* struct holding data bounds
 */
struct DataBounds
{
    float xmin, xmax, ymin, ymax, zmin, zmax, xExtent, yExtent, zExtent;
    int numLines;
};

void processData(const std::string& fileName)
{
    std::ifstream inFile{fileName};

    if (!inFile.is_open())
    {
        const auto msg = "Could not read file: " + fileName;
        std::cout << msg << std::endl;
        throw std::runtime_error(msg);
    }
    std::vector<Eigen::Vector3f> data{};  // array to store points in.

    // read data from file and get data bounds:
    const DataBounds bounds = readData(inFile, data);
    inFile.close();

    std::cout << bounds.xmin << " | " << bounds.xmax << " | " << bounds.ymin << " | " << bounds.ymax << " | " << bounds.
        xExtent << " | " << bounds.yExtent << std::endl;
    std::cout << "Number of vertices: " << bounds.numLines << std::endl;

    // define grid parameters:
    constexpr float stepLength = 10.f; // step length [m]
    const int numStepsX = static_cast<int>(ceil(bounds.xExtent / stepLength));
    const int numStepsY = static_cast<int>(ceil(bounds.yExtent / stepLength));

    // create 3D list for organizing height data in grid squares:
    std::vector<std::vector<std::vector<float>>> buckets(
        numStepsX,
        std::vector<std::vector<float>>(
            numStepsY,
            std::vector<float>()
        )
    );

    // create 3D list for holding the uniform grid after averaging of data
    std::vector<std::vector<Eigen::Vector3f>> grid(
        numStepsX,
        std::vector<Eigen::Vector3f>(
            numStepsY,
            Eigen::Vector3f()
        )
    );

    // mask to log empty squares, and offset-vector to center grid:
    std::vector<std::vector<bool>> fillMask(numStepsX, std::vector<bool>(numStepsY, false));
    Eigen::Vector3f offset{
        0.5f * static_cast<float>(numStepsX) * stepLength, 0.5f * static_cast<float>(numStepsY) * stepLength,
        0.5f * (bounds.zmax + bounds.zmin)
    };

    // store heights to grid squares in parallel using openMP:
#pragma omp parallel for  // NOLINT(clang-diagnostic-source-uses-openmp)
    for (int k = 0; k < static_cast<int>(data.size()); k++) // NOLINT(modernize-loop-convert)
    {
        Eigen::Vector3f point = data[k];
        int i = static_cast<int>((point.x() - bounds.xmin) / stepLength);
        int j = static_cast<int>((point.y() - bounds.ymin) / stepLength);

        if (i < 0) { i = 0; }
        else if (i >= numStepsX) { i = numStepsX - 1; }
        if (j < 0) { j = 0; }
        else if (j >= numStepsY) { j = numStepsY - 1; }

        // only one thread writes to array at once:
#pragma omp critical
        {
            buckets[i][j].emplace_back(point.z());
        }
    }

    data.clear();

    // perform calculation of mean height for each grid-square in parallel:
#pragma omp parallel for  // NOLINT(clang-diagnostic-source-uses-openmp)
    for (int i = 0; i < static_cast<int>(buckets.size()); ++i)
    {
        for (int j = 0; j < static_cast<int>(buckets[0].size()); ++j)
        {
            float meanHeight{};
            for (const auto& height : buckets[i][j])
            {
                meanHeight += height;
            }
            if (!buckets[i][j].empty())
            {
                meanHeight /= static_cast<float>(buckets[i][j].size());
                grid[i][j] = Eigen::Vector3f{  // place point of mean height in first corner of grid-square
                    static_cast<float>(i) * stepLength, static_cast<float>(j) * stepLength, meanHeight
                } - offset; // offset to center data on origin
                fillMask[i][j] = true;  // mark square as completed
            }
        }
    }

    buckets.clear();  // free no-longer needed memory

    for (int i = 0; i < static_cast<int>(grid.size()); ++i)
    {
        for (int j = 0; j < static_cast<int>(grid[0].size()); ++j)
        {
            if (fillMask[i][j])  // skip entries that already have averaged height
            {
                continue;
            }

            // if a square had no points to average, use average height of neighbours
            int numPoints{};

            for (int xn = i - 100; xn <= i + 100; ++xn)
            {
                if (xn < 0 || xn >= numStepsX) continue;
                for (int yn = j - 100; yn <= j + 100; ++yn)
                {
                    if (yn < 0 || yn >= numStepsY || !fillMask[xn][yn]) continue;

                    grid[i][j] += grid[xn][yn];
                    numPoints++;
                }
            }
            grid[i][j][2] /= numPoints > 0 ? static_cast<float>(numPoints) : 1.f;
            fillMask[i][j] = true;
        }
    }

    // write scaled down vertex data to file:
    writeVertexData("../ProcessedData/vertices.txt", grid, 0.5f, 0.5f, 0.5f);

    // generate and write regular triangle indexation to file:
    writeIndexFile("../ProcessedData/indices.txt", numStepsX, numStepsY);
}

void writeVertexData(const std::string& filePath, const std::vector<std::vector<Eigen::Vector3f>>& dataGrid,
                     const float scaleX, const float scaleY, const float scaleZ)
{
    std::ofstream outFile{filePath};
    if (!outFile.is_open())
    {
        const auto msg = "Could not open out-file: " + filePath;
        std::cout << msg << std::endl;
        throw std::runtime_error(msg);
    }

    // writing vertex data to file
    const size_t numVertices = dataGrid.size() * dataGrid[0].size();
    outFile << numVertices << "\n";

    for (size_t i = 0; i < dataGrid.size(); ++i)
    {
        for (size_t j = 0; j < dataGrid[0].size(); ++j)
        {
            auto point = dataGrid[i][j];
            outFile << "(" << point.x() * scaleX << ", " << point.z() * scaleZ << ", " << point.y() * scaleY << ")\n";
        }
    }

    outFile.close();
}

void writeIndexFile(const std::string& filePath, int numX, int numY)
{
    std::ofstream outFile{filePath};
    if (!outFile.is_open())
    {
        const auto msg = "Could not open out-file: " + filePath;
        std::cout << msg << std::endl;
        throw std::runtime_error(msg);
    }

    // useful constants:
    const int trianglesInARow = 2 * (numY - 1);
    const int totalTris = trianglesInARow * (numX - 1);

    std::vector<std::vector<int>> indices(totalTris, std::vector<int>(3, -1));
    std::vector<std::vector<int>> neighbours(totalTris, std::vector<int>(3, -1));

    // loop through grid squares and index each triangle in square:
#pragma omp parallel for  // done in parallel as iterations are independent of each other
    for (int i = 0; i < numX - 1; i++)
    {
        // useful constant:
        const int numTrianglesUptoThisRow = 2 * i * (numY - 1);
        for (int j = 0; j < numY - 1; j++)
        {
            // useful constants
            const int evenTriangle = 2 * (i * (numY - 1) + j);
            const int oddTriangle = evenTriangle + 1;

            // first triangle
            indices[evenTriangle][0] = j + i * numY;
            indices[evenTriangle][1] = j + 1 + i * numY;
            indices[evenTriangle][2] = j + (i + 1) * numY;

            // calculate neighbour-triangles and set to -1 if out of bounds:
            int T0 = oddTriangle;
            neighbours[evenTriangle][0] = T0 < numTrianglesUptoThisRow + trianglesInARow ? T0 : -1;

            int T1 = evenTriangle - 1;
            neighbours[evenTriangle][1] = T1 > numTrianglesUptoThisRow ? T1 : -1;

            int T2 = evenTriangle - trianglesInARow + 1;
            neighbours[evenTriangle][2] = T2 > 0 ? T2 : -1;

            // second triangle
            indices[oddTriangle][0] = j + 1 + i * numY;
            indices[oddTriangle][1] = j + 1 + (i + 1) * numY;
            indices[oddTriangle][2] = j + (i + 1) * numY;

            // calculate neighbour-triangles and set to -1 if out of bounds:
            T0 = evenTriangle + trianglesInARow;
            neighbours[oddTriangle][0] = T0 < totalTris ? T0 : -1;

            T1 = evenTriangle;
            neighbours[oddTriangle][1] = T1 >= numTrianglesUptoThisRow ? T1 : -1;

            T2 = oddTriangle + 1;
            neighbours[oddTriangle][2] = T2 < numTrianglesUptoThisRow + trianglesInARow ? T2 : -1;
        }
    }


    // writing indexation data to file
    outFile << totalTris << "\n";
    std::cout << indices.size() << " " << totalTris << std::endl;

    for (size_t i = 0; i < indices.size(); i++)
    {
        outFile << indices[i][0] << " " << indices[i][1] << " " << indices[i][2] << " "
            << neighbours[i][0] << " " << neighbours[i][1] << " " << neighbours[i][2] << "\n";
    }

    outFile.close();
}

DataBounds readData(std::ifstream& file, std::vector<Eigen::Vector3f>& dataContainer)
{
    dataContainer.clear();
    file.clear();
    file.seekg(0);

    float xmin, ymin, zmin, x{}, y{}, z{};

    file >> x >> y >> z;
    float xmax = xmin = x;
    float ymax = ymin = y;
    float zmax = zmin = z;
    int numLines{1};

    // read data from provided filestream and store in provided container-vector.
    while (file >> x >> y >> z)
    {
        numLines++;

        const auto xIsMax{xmax < x};
        xmax = x * static_cast<float>(xIsMax) + static_cast<float>(!xIsMax) * xmax;

        const auto xIsMin{xmin > x};
        xmin = x * static_cast<float>(xIsMin) + static_cast<float>(!xIsMin) * xmin;

        const auto yIsMax{ymax < y};
        ymax = y * static_cast<float>(yIsMax) + static_cast<float>(!yIsMax) * ymax;

        const auto yIsMin{ymin > y};
        ymin = y * static_cast<float>(yIsMin) + static_cast<float>(!yIsMin) * ymin;

        const auto zIsMax{zmax < z};
        zmax = z * static_cast<float>(zIsMax) + static_cast<float>(!zIsMax) * zmax;

        const auto zIsMin{zmin > z};
        zmin = z * static_cast<float>(zIsMin) + static_cast<float>(!zIsMin) * zmin;

        dataContainer.emplace_back(x, y, z);
    }

    // return bounds of data in struct.
    return DataBounds{xmin, xmax, ymin, ymax, zmin, zmax, xmax - xmin, ymax - ymin, zmax - zmin, numLines};
}

int main(int argc, char* argv[])
{
    processData("../RawData/merged.txt");
    return 0;
}
