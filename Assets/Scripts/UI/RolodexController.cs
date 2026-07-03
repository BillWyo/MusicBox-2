using UnityEngine;
using TMPro;

public class RolodexController : MonoBehaviour
{
    [SerializeField] private MonoBehaviour _dataSource;
    [SerializeField] private int _visibleTiles = 9;
    [SerializeField] private float _radius = 3f;
    [SerializeField] private float _angleStep = 15f;
    [SerializeField] private float _tileHeight = 0f;
    [SerializeField] private float _tileWidth = 1f;
    [SerializeField] private float _tileHeight_mesh = 1f;
    [SerializeField] private Color _tileColor = new Color(0.4f, 0.4f, 0.4f, 1f);
    [SerializeField] private ModeController.Mode _activeInMode = ModeController.Mode.Browse;

    public event System.Action<int> OnItemSelected;
    public event System.Action<int, int> OnIndexChanged; // (currentIndex, totalCount)

    private int _offset;
    private GameObject[] _tiles;
    private float _scrollCooldown = 0.15f;
    private float _lastScrollTime;

    private ITileDataSource DataSource => _dataSource as ITileDataSource;

    public ITileDataSource PublicDataSource => DataSource;

    public void SetActiveMode(ModeController.Mode mode)
    {
        _activeInMode = mode;
    }

    public void SetDataSource(MonoBehaviour dataSource)
    {
        _dataSource = dataSource;
        if (_tiles != null)
        {
            foreach (var tile in _tiles)
            {
                if (tile != null) Destroy(tile);
            }
            _tiles = null;
        }
    }

    public int CurrentIndex
    {
        get
        {
            if (DataSource == null || DataSource.Count == 0) return 0;
            int centerIndex = _visibleTiles / 2;
            return (_offset + centerIndex) % DataSource.Count;
        }
    }

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
            XRInputManager.Instance.OnJoystickTap += OnJoystickTap;
            XRInputManager.Instance.OnJoystickHold += OnJoystickHold;
            XRInputManager.Instance.OnRightTriggerPressed += OnSelectPressed;
        }

        if (_tiles == null && DataSource != null && DataSource.Count > 0)
            RefreshTiles();
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

            meshFilter.mesh = CreateQuadMesh(_tileWidth, _tileHeight_mesh);
            Shader unlitShader = Shader.Find("Unlit/Color");
            Material mat = new Material(unlitShader != null ? unlitShader : Shader.Find("Standard"));
            mat.color = _tileColor;
            meshRenderer.material = mat;
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
        OnIndexChanged?.Invoke(CurrentIndex, DataSource.Count);
    }

    void OnJoystickTap(int direction)
    {
        if (!gameObject.activeSelf || ModeController.Instance.CurrentMode != _activeInMode) return;
        if (DataSource == null) return;
        if (Time.time - _lastScrollTime < _scrollCooldown) return;

        int count = DataSource.Count;
        if (count == 0) return;

        // Single step advance/back on tap
        _offset = (_offset + direction + count) % count;
        _lastScrollTime = Time.time;
        RefreshTiles();
        OnIndexChanged?.Invoke(CurrentIndex, count);
    }

    void OnJoystickHold(int direction)
    {
        if (!gameObject.activeSelf || ModeController.Instance.CurrentMode != _activeInMode) return;
        if (DataSource == null) return;
        if (Time.time - _lastScrollTime < _scrollCooldown) return;

        int count = DataSource.Count;
        if (count == 0) return;

        // Rapid scroll on hold: advance 3 items per hold event
        int rapidAdvance = direction * 3;
        _offset = (_offset + rapidAdvance + count * 10) % count;
        _lastScrollTime = Time.time;
        RefreshTiles();
        OnIndexChanged?.Invoke(CurrentIndex, count);
    }

    void OnSelectPressed()
    {
        if (!gameObject.activeSelf || ModeController.Instance.CurrentMode != _activeInMode) return;
        if (DataSource == null || DataSource.Count == 0) return;

        int centerIndex = _visibleTiles / 2;
        int selectedIndex = (_offset + centerIndex) % DataSource.Count;
        OnItemSelected?.Invoke(selectedIndex);
    }

    Mesh CreateQuadMesh(float width, float height)
    {
        Mesh mesh = new Mesh();
        float hw = width / 2f;
        float hh = height / 2f;

        Vector3[] vertices = new Vector3[4]
        {
            new Vector3(-hw, -hh, 0),
            new Vector3(hw, -hh, 0),
            new Vector3(hw, hh, 0),
            new Vector3(-hw, hh, 0)
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
            XRInputManager.Instance.OnJoystickTap -= OnJoystickTap;
            XRInputManager.Instance.OnJoystickHold -= OnJoystickHold;
            XRInputManager.Instance.OnRightTriggerPressed -= OnSelectPressed;
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
