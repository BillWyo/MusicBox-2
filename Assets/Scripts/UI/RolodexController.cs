using UnityEngine;
using TMPro;

public class RolodexController : MonoBehaviour
{
    [SerializeField] private MonoBehaviour _dataSource;
    [SerializeField] private int _visibleTiles = 9;
    [SerializeField] private float _radius = 3f;
    [SerializeField] private float _angleStep = 15f;
    [SerializeField] private float _tileHeight = 0f;

    public event System.Action<int> OnItemSelected;

    private int _offset;
    private GameObject[] _tiles;

    private ITileDataSource DataSource => _dataSource as ITileDataSource;

    void Start()
    {
        if (_dataSource is AlbumDataSource albumSource)
        {
            albumSource.OnDataChanged += RefreshTiles;
        }
        else if (_dataSource is PlaylistDataSource playlistSource)
        {
            playlistSource.OnDataChanged += RefreshTiles;
        }
        else if (_dataSource is TrackListDataSource trackSource)
        {
            trackSource.OnDataChanged += RefreshTiles;
        }

        if (XRInputManager.Instance != null)
        {
            XRInputManager.Instance.OnJoystickMoved += OnJoystickMoved;
            XRInputManager.Instance.OnAButtonPressed += OnAButtonPressed;
        }
    }

    void CreateTiles()
    {
        _tiles = new GameObject[_visibleTiles];
        int count = DataSource.Count;
        if (count == 0) return;

        for (int i = 0; i < _visibleTiles; i++)
        {
            GameObject tile = new GameObject($"Tile_{i}");
            tile.transform.SetParent(transform);

            MeshFilter meshFilter = tile.AddComponent<MeshFilter>();
            MeshCollider meshCollider = tile.AddComponent<MeshCollider>();
            MeshRenderer meshRenderer = tile.AddComponent<MeshRenderer>();

            meshFilter.mesh = CreateQuadMesh();
            meshRenderer.material = new Material(Shader.Find("Standard"));
            meshCollider.convex = true;

            GameObject textObj = new GameObject("TextLabel");
            textObj.transform.SetParent(tile.transform);
            textObj.transform.localPosition = Vector3.zero;

            TextMeshPro textMesh = textObj.AddComponent<TextMeshPro>();
            textMesh.text = DataSource.GetTitle((i + _offset) % count);
            textMesh.fontSize = 4;
            textMesh.alignment = TextAlignmentOptions.Center;

            _tiles[i] = tile;
            UpdateTilePosition(i);
        }
    }

    void UpdateTilePosition(int visibleIndex)
    {
        if (DataSource.Count == 0 || _tiles == null || _tiles[visibleIndex] == null) return;
        int dataIndex = (_offset + visibleIndex) % DataSource.Count;
        int centerIndex = _visibleTiles / 2;

        float angle = (visibleIndex - centerIndex) * _angleStep * Mathf.Deg2Rad;
        float x = _radius * Mathf.Sin(angle);
        float z = _radius * Mathf.Cos(angle);

        _tiles[visibleIndex].transform.localPosition = new Vector3(x, _tileHeight, z);
        _tiles[visibleIndex].transform.LookAt(transform.position + Vector3.up * _tileHeight);

        TextMeshPro textMesh = _tiles[visibleIndex].GetComponentInChildren<TextMeshPro>();
        if (textMesh != null)
        {
            string title = DataSource.GetTitle(dataIndex);
            string subtitle = DataSource.GetSubtitle(dataIndex);
            textMesh.text = $"{title}\n{subtitle}";

            if (visibleIndex == centerIndex)
                textMesh.color = Color.yellow;
            else
                textMesh.color = Color.white;
        }
    }

    void RefreshTiles()
    {
        if (_tiles == null || DataSource.Count == 0) CreateTiles();
        else
        {
            for (int i = 0; i < _visibleTiles; i++)
            {
                UpdateTilePosition(i);
            }
        }
    }

    void OnJoystickMoved(Vector2 value)
    {
        if (!gameObject.activeSelf) return;
        if (DataSource == null) return;

        int count = DataSource.Count;
        if (count == 0) return;

        int direction = 0;
        if (value.x < -0.5f) direction = -1;
        else if (value.x > 0.5f) direction = 1;

        if (direction != 0)
        {
            _offset = (_offset + direction + count) % count;
            RefreshTiles();
        }
    }

    void OnAButtonPressed()
    {
        if (!gameObject.activeSelf) return;
        if (DataSource == null || DataSource.Count == 0) return;

        int centerIndex = _visibleTiles / 2;
        int selectedIndex = (_offset + centerIndex) % DataSource.Count;
        OnItemSelected?.Invoke(selectedIndex);
    }

    Mesh CreateQuadMesh()
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[4]
        {
            new Vector3(-0.5f, -0.5f, 0),
            new Vector3(0.5f, -0.5f, 0),
            new Vector3(0.5f, 0.5f, 0),
            new Vector3(-0.5f, 0.5f, 0)
        };

        int[] triangles = new int[6]
        {
            0, 2, 1,
            0, 3, 2
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        return mesh;
    }

    void OnDestroy()
    {
        if (XRInputManager.Instance != null)
        {
            XRInputManager.Instance.OnJoystickMoved -= OnJoystickMoved;
            XRInputManager.Instance.OnAButtonPressed -= OnAButtonPressed;
        }

        if (_dataSource is AlbumDataSource albumSource)
        {
            albumSource.OnDataChanged -= RefreshTiles;
        }
        else if (_dataSource is PlaylistDataSource playlistSource)
        {
            playlistSource.OnDataChanged -= RefreshTiles;
        }
        else if (_dataSource is TrackListDataSource trackSource)
        {
            trackSource.OnDataChanged -= RefreshTiles;
        }
    }
}
