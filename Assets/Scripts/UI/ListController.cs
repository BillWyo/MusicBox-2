using UnityEngine;
using TMPro;

public class ListController : MonoBehaviour
{
    [SerializeField] private MonoBehaviour _dataSource;
    [SerializeField] private int _visibleItems = 6;
    [SerializeField] private float _itemHeight = 0.5f;
    [SerializeField] private float _itemWidth = 2f;
    [SerializeField] private float _listDistance = 5f;
    [SerializeField] private Color _panelBackgroundColor = new Color(0.25f, 0.25f, 0.25f, 1f);
    [SerializeField] private Color _itemColor = new Color(0.4f, 0.4f, 0.4f, 1f);
    [SerializeField] private Color _textColor = Color.white;
    [SerializeField] private Color _textCenterColor = Color.yellow;
    [SerializeField] private float _panelPadding = 0.5f;
    [SerializeField] private float _quadScale = 1f;
    [SerializeField] private float _quadWidth = 1f;
    [SerializeField] private float _quadHeight = 1f;
    [SerializeField] private float _quadZOffset = 0f;
    [SerializeField] private float _textZOffset = 2f;
    [SerializeField] private int _textFontSize = 18;

    private IListDataSource DataSource => _dataSource as IListDataSource;
    private Material _panelBackgroundMaterial;
    private Color _lastPanelBackgroundColor;
    private GameObject _focusBorder;
    private bool _isFocused;
    private GameObject _headerObj;
    private TextMeshPro _headerText;
    private bool _panelCreated;

    public event System.Action<int> OnItemSelected;

    public void SetFocused(bool focused)
    {
        _isFocused = focused;
        if (_focusBorder != null)
            _focusBorder.SetActive(focused);
    }

    public void SetHeader(string title)
    {
        if (_headerText == null)
        {
            CreatePanelBackground();
        }
        if (_headerText != null)
        {
            _headerText.text = title;
        }
    }

    // Command interface
    public void ScrollUp()
    {
        if (!gameObject.activeSelf || _dataSource == null || DataSource.Count == 0) return;
        if (Time.time - _lastScrollTime < _scrollCooldown) return;

        int count = DataSource.Count;
        _selectedIndex = Mathf.Max(0, _selectedIndex - 1);

        // Scroll if selection moves above visible range
        if (_selectedIndex < _offset)
            _offset = _selectedIndex;

        _lastScrollTime = Time.time;
        RefreshItems();
    }

    public void ScrollDown()
    {
        if (!gameObject.activeSelf || _dataSource == null || DataSource.Count == 0) return;
        if (Time.time - _lastScrollTime < _scrollCooldown) return;

        int count = DataSource.Count;
        _selectedIndex = Mathf.Min(count - 1, _selectedIndex + 1);

        // Scroll if selection moves below visible range
        if (_selectedIndex >= _offset + _visibleItems)
            _offset = Mathf.Max(0, _selectedIndex - _visibleItems + 1);

        _lastScrollTime = Time.time;
        RefreshItems();
    }

    public void SelectCenter()
    {
        if (!gameObject.activeSelf || _dataSource == null || DataSource.Count == 0) return;
        OnItemSelected?.Invoke(_selectedIndex);
    }

    public int GetSelectedIndex()
    {
        if (_dataSource == null || DataSource.Count == 0) return -1;
        return _selectedIndex;
    }

    public void CenterOnLast()
    {
        if (_dataSource == null || DataSource.Count == 0) return;
        int count = DataSource.Count;
        _selectedIndex = count - 1;
        int centerOffset = Mathf.Max(0, _selectedIndex - _visibleItems / 2);
        _offset = Mathf.Min(centerOffset, Mathf.Max(0, count - _visibleItems));
        RefreshItems();
    }

    public void ResetView()
    {
        _offset = 0;
        _selectedIndex = 0;
        if (_items != null)
        {
            foreach (var item in _items)
            {
                if (item != null) Destroy(item);
            }
            _items = null;
        }
        RefreshItems();
    }

    public void SetDataSource(MonoBehaviour dataSource)
    {
        if (_dataSource != null)
            DataSource.OnDataChanged -= RefreshItems;

        _dataSource = dataSource;
        _offset = 0;
        _selectedIndex = 0;

        if (_dataSource != null)
            DataSource.OnDataChanged += RefreshItems;

        if (_items != null)
        {
            foreach (var item in _items)
            {
                if (item != null) Destroy(item);
            }
            _items = null;
        }
    }

    private int _offset;
    private int _selectedIndex;
    private GameObject[] _items;
    private float _scrollCooldown = 0.15f;
    private float _lastScrollTime;

    void Start()
    {
        if (_dataSource != null && _visibleItems == 0)
            _visibleItems = 6;

        if (!_panelCreated)
            CreatePanelBackground();

        if (_dataSource != null)
        {
            DataSource.OnDataChanged += RefreshItems;
            Debug.Log("ListController subscribed to OnDataChanged");
            if (_items == null && DataSource.Count > 0)
                RefreshItems();
        }
        else
        {
            Debug.LogError("ListController: _dataSource is null in Start()!");
        }
    }

    void Update()
    {
        if (_panelBackgroundMaterial != null && _panelBackgroundColor != _lastPanelBackgroundColor)
        {
            Debug.Log($"Panel color changed from {_lastPanelBackgroundColor} to {_panelBackgroundColor}");
            _panelBackgroundMaterial.color = _panelBackgroundColor;
            _lastPanelBackgroundColor = _panelBackgroundColor;
        }
    }

    void CreatePanelBackground()
    {
        if (_panelCreated) return;

        GameObject bg = new GameObject("PanelBackground");
        bg.transform.SetParent(transform);
        bg.transform.localPosition = new Vector3(0, 0, 10f);

        MeshFilter meshFilter = bg.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = bg.AddComponent<MeshRenderer>();

        float halfHeight = (_visibleItems * _itemHeight / 2) + _panelPadding;
        float halfWidth = (_itemWidth / 2) + _panelPadding;

        Mesh bgMesh = new Mesh();
        Vector3[] verts = new Vector3[4]
        {
            new Vector3(-halfWidth, -halfHeight, 0),
            new Vector3(halfWidth, -halfHeight, 0),
            new Vector3(halfWidth, halfHeight, 0),
            new Vector3(-halfWidth, halfHeight, 0)
        };
        bgMesh.vertices = verts;
        bgMesh.triangles = new int[6] { 0, 2, 1, 0, 3, 2 };
        bgMesh.RecalculateNormals();
        meshFilter.mesh = bgMesh;

        Shader unlitShader = Shader.Find("Unlit/Color");
        Debug.Log($"Unlit shader found: {unlitShader != null}");
        _panelBackgroundMaterial = new Material(unlitShader != null ? unlitShader : Shader.Find("Standard"));
        _panelBackgroundMaterial.color = _panelBackgroundColor;
        meshRenderer.material = _panelBackgroundMaterial;
        _lastPanelBackgroundColor = _panelBackgroundColor;
        Debug.Log($"Panel background created with color: {_panelBackgroundColor}, material: {_panelBackgroundMaterial}");

        CreateFocusBorder(halfWidth, halfHeight);
        CreateHeader();
        _panelCreated = true;
    }

    void CreateFocusBorder(float halfWidth, float halfHeight)
    {
        _focusBorder = new GameObject("FocusBorder");
        _focusBorder.transform.SetParent(transform);
        _focusBorder.transform.localPosition = new Vector3(0, 0, 9.5f);
        _focusBorder.SetActive(false);

        MeshFilter meshFilter = _focusBorder.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = _focusBorder.AddComponent<MeshRenderer>();

        Mesh borderMesh = new Mesh();
        float borderThickness = 0.1f;

        // Create a border frame (4 quads around edges)
        Vector3[] verts = new Vector3[16]
        {
            // Top border
            new Vector3(-halfWidth - borderThickness, halfHeight, 0),
            new Vector3(halfWidth + borderThickness, halfHeight, 0),
            new Vector3(halfWidth + borderThickness, halfHeight + borderThickness, 0),
            new Vector3(-halfWidth - borderThickness, halfHeight + borderThickness, 0),

            // Bottom border
            new Vector3(-halfWidth - borderThickness, -halfHeight - borderThickness, 0),
            new Vector3(halfWidth + borderThickness, -halfHeight - borderThickness, 0),
            new Vector3(halfWidth + borderThickness, -halfHeight, 0),
            new Vector3(-halfWidth - borderThickness, -halfHeight, 0),

            // Left border
            new Vector3(-halfWidth - borderThickness, -halfHeight, 0),
            new Vector3(-halfWidth, -halfHeight, 0),
            new Vector3(-halfWidth, halfHeight, 0),
            new Vector3(-halfWidth - borderThickness, halfHeight, 0),

            // Right border
            new Vector3(halfWidth, -halfHeight, 0),
            new Vector3(halfWidth + borderThickness, -halfHeight, 0),
            new Vector3(halfWidth + borderThickness, halfHeight, 0),
            new Vector3(halfWidth, halfHeight, 0)
        };

        int[] triangles = new int[24]
        {
            0, 2, 1, 0, 3, 2,  // Top
            4, 5, 6, 4, 6, 7,  // Bottom
            8, 10, 9, 8, 11, 10,  // Left
            12, 13, 14, 12, 14, 15  // Right
        };

        borderMesh.vertices = verts;
        borderMesh.triangles = triangles;
        borderMesh.RecalculateNormals();
        meshFilter.mesh = borderMesh;

        Shader unlitShader = Shader.Find("Unlit/Color");
        Material borderMat = new Material(unlitShader != null ? unlitShader : Shader.Find("Standard"));
        borderMat.color = new Color(0, 1, 1, 1); // Cyan highlight
        meshRenderer.material = borderMat;
    }

    void CreateHeader()
    {
        _headerObj = new GameObject("Header");
        _headerObj.transform.SetParent(transform);

        float headerHeight = (_visibleItems * _itemHeight / 2) + _itemHeight + 0.5f;
        _headerObj.transform.localPosition = new Vector3(0, headerHeight, 0);

        _headerText = _headerObj.AddComponent<TextMeshPro>();
        _headerText.text = "Playlist";
        _headerText.fontSize = 20;
        _headerText.alignment = TextAlignmentOptions.Center;
        _headerText.color = Color.white;

        RectTransform headerRect = _headerObj.GetComponent<RectTransform>();
        if (headerRect != null)
            headerRect.sizeDelta = new Vector2(_itemWidth, _itemHeight);
    }

    void CreateItems()
    {
        int count = DataSource.Count;
        if (count == 0)
        {
            _items = null;
            return;
        }

        _items = new GameObject[_visibleItems];

        for (int i = 0; i < _visibleItems; i++)
        {
            GameObject item = new GameObject($"ListItem_{i}");
            item.transform.SetParent(transform);

            MeshFilter meshFilter = item.AddComponent<MeshFilter>();
            MeshCollider meshCollider = item.AddComponent<MeshCollider>();
            MeshRenderer meshRenderer = item.AddComponent<MeshRenderer>();

            meshFilter.mesh = CreateQuadMesh();
            Shader unlitShader = Shader.Find("Unlit/Color");
            Material mat = new Material(unlitShader != null ? unlitShader : Shader.Find("Standard"));
            mat.color = _itemColor;
            meshRenderer.material = mat;
            meshCollider.convex = true;
            item.transform.localScale = new Vector3(_quadScale, _quadScale, 1f);

            GameObject textObj = new GameObject("TextLabel");
            textObj.transform.SetParent(item.transform);
            textObj.transform.localPosition = new Vector3(0, 0, _textZOffset);

            TextMeshPro textMesh = textObj.AddComponent<TextMeshPro>();
            textMesh.text = DataSource.GetTitle((i + _offset) % count);
            textMesh.fontSize = _textFontSize;
            textMesh.alignment = TextAlignmentOptions.Center;

            if (textMesh.fontSharedMaterial != null)
            {
                textMesh.fontSharedMaterial.renderQueue = 5000;
            }

            _items[i] = item;
            UpdateItemPosition(i);
        }
    }

    void UpdateItemPosition(int visibleIndex)
    {
        if (DataSource.Count == 0 || _items == null || visibleIndex >= _items.Length) return;
        if (_items[visibleIndex] == null) return;

        int dataIndex = _offset + visibleIndex;

        // Don't render beyond data bounds
        if (dataIndex >= DataSource.Count)
        {
            _items[visibleIndex].SetActive(false);
            return;
        }

        _items[visibleIndex].SetActive(true);

        float yOffset = ((_visibleItems - 1) * _itemHeight / 2f) - (visibleIndex * _itemHeight);
        _items[visibleIndex].transform.localPosition = new Vector3(0, yOffset, _listDistance + _quadZOffset - 5f);
        _items[visibleIndex].transform.rotation = Quaternion.identity;

        TextMeshPro textMesh = _items[visibleIndex].GetComponentInChildren<TextMeshPro>();
        if (textMesh != null)
        {
            string title = DataSource.GetTitle(dataIndex);
            textMesh.text = title;

            if (dataIndex == _selectedIndex)
                textMesh.color = _textCenterColor;
            else
                textMesh.color = _textColor;
        }
    }

    void RefreshItems()
    {
        Debug.Log($"ListController.RefreshItems called, _items={_items == null}, count={DataSource.Count}");
        if (_items == null || DataSource.Count == 0) CreateItems();
        else
        {
            for (int i = 0; i < _visibleItems; i++)
            {
                UpdateItemPosition(i);
            }
        }
    }


    Mesh CreateQuadMesh()
    {
        Mesh mesh = new Mesh();
        float hw = _quadWidth / 2f;
        float hh = _quadHeight / 2f;

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
        if (_dataSource != null)
            DataSource.OnDataChanged -= RefreshItems;
    }
}
