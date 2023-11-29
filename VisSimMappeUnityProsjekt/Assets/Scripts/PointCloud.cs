using System;
using System.Globalization;
using TMPro;
using UnityEngine;

public enum DataSet
{
    Sampled,
    Averaged
}

public class PointCloud : MonoBehaviour
{
    [SerializeField] private TextAsset sampledPointData;
    [SerializeField] private TextAsset averagedPointData;
    [SerializeField] private TMP_Text buttonText;
    
    [SerializeField] private Vector3 offset;
    [SerializeField] private Vector3 scale = Vector3.one;
    
    private Vector3[] _vertices = {new Vector3(0,0,0), new Vector3(1, 1, 1)};
    private Vector2 _minMaxVec;
    private ComputeBuffer _pointBuffer;
    private DataSet _dataSet = DataSet.Sampled;
    
    private const int CommandCount = 1;
    public Material material;
    public Mesh mesh;

    private GraphicsBuffer _commandBuf;
    private GraphicsBuffer.IndirectDrawIndexedArgs[] _commandData;
    private static readonly int ObjectToWorld = Shader.PropertyToID("_ObjectToWorld");
    private static readonly int PositionUniform = Shader.PropertyToID("_positions");
    private static readonly int HeightMinMaxUniform = Shader.PropertyToID("_minMaxHeight");

    private void SwitchButtonText()
    {
        buttonText.text = _dataSet switch
        {
            DataSet.Sampled => "Sampled",
            DataSet.Averaged => "Averaged",
            _ => "null"
        };
    }

    private void Start()
    {
        var vertexFile = _dataSet switch
        {
            DataSet.Sampled => sampledPointData,
            DataSet.Averaged => averagedPointData,
            _ => null
        };

        SwitchButtonText();

        if (vertexFile == null) return;
        
        ReadVertexData(vertexFile);
        Debug.LogWarning(_vertices.Length);
        
        _commandBuf = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, CommandCount,
            GraphicsBuffer.IndirectDrawIndexedArgs.size);
        _commandData = new GraphicsBuffer.IndirectDrawIndexedArgs[CommandCount];
        _pointBuffer = new ComputeBuffer(_vertices.Length, 4 * 3);
        _pointBuffer.SetData(_vertices);
    }

    private void Update()
    {
        var rp = new RenderParams(material)
        {
            worldBounds = new Bounds(Vector3.zero, 10000 * Vector3.one), // use tighter bounds for better FOV culling
            matProps = new MaterialPropertyBlock()
        };
        var uniformMat = Matrix4x4.identity;
        uniformMat.SetTRS(-offset, Quaternion.identity, scale);
        
        rp.matProps.SetMatrix(ObjectToWorld, uniformMat);
        rp.matProps.SetBuffer(PositionUniform, _pointBuffer);
        rp.matProps.SetVector(HeightMinMaxUniform, new Vector2(_minMaxVec.x, _minMaxVec.y));
        
        _commandData[0].indexCountPerInstance = mesh.GetIndexCount(0);
        _commandData[0].instanceCount = (uint)_vertices.Length;
        _commandBuf.SetData(_commandData);
        Graphics.RenderMeshIndirect(rp, mesh, _commandBuf, CommandCount);
    }

    public void OnSwitchDataSet()
    {
        _dataSet = _dataSet switch
        {
            DataSet.Sampled => DataSet.Averaged,
            DataSet.Averaged => DataSet.Sampled,
            _ => throw new ArgumentOutOfRangeException()
        };
        
        var vertexFile = _dataSet switch
        {
            DataSet.Sampled => sampledPointData,
            DataSet.Averaged => averagedPointData,
            _ => null
        };

        SwitchButtonText();

        if (vertexFile == null) return;
        
        ReadVertexData(vertexFile);
        
        _commandBuf = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, CommandCount,
            GraphicsBuffer.IndirectDrawIndexedArgs.size);
        _commandData = new GraphicsBuffer.IndirectDrawIndexedArgs[CommandCount];
        _pointBuffer = new ComputeBuffer(_vertices.Length, 4 * 3);
        _pointBuffer.SetData(_vertices);
    }

    private void OnDestroy()
    {
        _commandBuf?.Release();
        _commandBuf = null;
    }

    private void ReadVertexData(TextAsset vertexData)
    {
        // defines which characters to split file into lines on:
        var fileDelimiters = new[] { "\r\n", "\r", "\n" };
    
        // defines which characters to split each line on:
        var lineDelimiters = new[] { '(',')',',' };
    
        // split file into array of non-empty lines:
        var lines = vertexData.text.Split(fileDelimiters, StringSplitOptions.RemoveEmptyEntries);
    
        if (lines.Length < 1)
        {
            Debug.LogWarning($"{vertexData.name} was empty!");
            return;
        }
    
        var numVertices = int.Parse(lines[0]);
    
        if (numVertices < 1)
        {
            Debug.LogWarning($"{vertexData.name} contains no vertex data!");
            return;
        }
    
        var vertices = new Vector3[numVertices];
    
        for (var i = 1; i <= numVertices; i++)
        {
            // split line and read coordinates:
            var elements = lines[i].Split(lineDelimiters, StringSplitOptions.RemoveEmptyEntries);
            if (elements.Length < 3)
            {
                Debug.LogWarning($"{vertexData.name} is missing data on line {i}");
                continue;
            }
    
            vertices[i - 1] = new Vector3(
                float.Parse(elements[0], CultureInfo.InvariantCulture),
                float.Parse(elements[1], CultureInfo.InvariantCulture),
                float.Parse(elements[2], CultureInfo.InvariantCulture)
            );
        }
        
        if (vertices.Length < 1) return;
        
        _minMaxVec =  new Vector2(vertices[0].y, vertices[0].y);
        foreach (var vertex in vertices)
        {
            if (_minMaxVec.x > vertex.y)
            {
                _minMaxVec.x = vertex.y;
            }
            if (_minMaxVec.y < vertex.y)
            {
                _minMaxVec.y = vertex.y;
            }
        }
    
        _vertices = vertices;
    }
}