using UnityEngine;
using TMPro;

public class ListController : MonoBehaviour
{
    [SerializeField] private MonoBehaviour _dataSource;
    [SerializeField] private int _visibleItems = 6;
    [SerializeField] private float _itemHeight = 0.5f;
    [SerializeField] private float _itemWidth = 2f;
    [SerializeField] private float _listDistance = 5f;
    [SerializeField] private Color _panelBackgroundColor = new Color(0.15f, 0.15f, 0.15f, 1f);
    [SerializeField] private Color _itemColor = new Color(0.4f, 0.4f, 0.4f, 1f);
    [SerializeField] private float _panelPadding = 0.5f;
    [SerializeField] private float _quadScale = 1f;
    [SerializeField] private float _quadWidth = 1f;
    [SerializeField] private float _quadHeight = 1f;
    [SerializeField] private float _quadZOffset = 0f;
    [SerializeField] private int _textFontSize = 18;

    private IListDataSource DataSource => _dataSource as IListDataSource;

    public event System.Action<int> OnItemSelected;

    // Command interface
    public void ScrollUp()
    {
        if (!gameObject.activeSelf || _dataSource == null || DataSource.Count == 0) return;
        int count = DataSource.Count;
        _offset = (_offset - 1 + count) % count;
        RefreshItems();
    }

    public void ScrollDown()
    {
        if (!gameObject.activeSelf || _dataSource == null || DataSource.Count == 0) return;
        int count = DataSource.Count;
        _offset = (_offset + 1) % count;
        RefreshItems();
    }

    public void SelectCenter()
    {
        if (!gameObject.activeSelf || _dataSource == null || DataSource.Count == 0) return;
        int centerIndex = _visibleItems / 2;
        int selectedIndex = (_offset + centerIndex) % DataSource.Count;
        OnItemSelected?.Invoke(selectedIndex);
    }

    public int GetSelectedIndex()
    {
        if (_dataSource == null || DataSource.Count == 0) return -1;
        int centerIndex = _visibleItems / 2;
        return (_offset + centerIndex) % DataSource.Count;
    }

    public void SetDataSource(MonoBehaviour dataSource)
    {
        if (_dataSource != null)
            DataSource.OnDataChanged -= RefreshItems;

        _dataSource = dataSource;
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
    private GameObject[] _items;

    void Start()
    {
        if (_dataSource != null && _visibleItems == 0)
            _visibleItems = 6;

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

    void CreatePanelBackground()
    {
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

        Material bgMat = new Material(Shader.Find("Standard"));
        bgMat.color = _panelBackgroundColor;
        bgMat.renderQueue = 1000;  // Render behind everything else
        meshRenderer.material = bgMat;
    }

    void CreateItems()
    {
        _items = new GameObject[_visibleItems];
        int count = DataSource.Count;
        if (count == 0) return;

        for (int i = 0; i < _visibleItems; i++)
        {
            GameObject item = new GameObject($"ListItem_{i}");
            item.transform.SetParent(transform);

            MeshFilter meshFilter = item.AddComponent<MeshFilter>();
            MeshCollider meshCollider = item.AddComponent<MeshCollider>();
            MeshRenderer meshRenderer = item.AddComponent<MeshRenderer>();

            meshFilter.mesh = CreateQuadMesh();
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = _itemColor;
            meshRenderer.material = mat;
            meshCollider.convex = true;
            item.transform.localScale = new Vector3(_quadScale, _quadScale, 1f);

            GameObject textObj = new GameObject("TextLabel");
            textObj.transform.SetParent(item.transform);
            textObj.transform.localPosition = new Vector3(0, 0, 2f);

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
        if (DataSource.Count == 0 || _items == null || _items[visibleIndex] == null) return;
        int dataIndex = (_offset + visibleIndex) % DataSource.Count;
        int centerIndex = _visibleItems / 2;

        float yOffset = (centerIndex - visibleIndex) * _itemHeight;
        _items[visibleIndex].transform.localPosition = new Vector3(0, yOffset, _listDistance + _quadZOffset - 5f);

        Camera cam = Camera.main;
        if (cam != null)
        {
            Vector3 dirToCamera = cam.transform.position - _items[visibleIndex].transform.position;
            _items[visibleIndex].transform.rotation = Quaternion.LookRotation(dirToCamera) * Quaternion.Euler(0, 180, 0);
        }

        TextMeshPro textMesh = _items[visibleIndex].GetComponentInChildren<TextMeshPro>();
        if (textMesh != null)
        {
            string title = DataSource.GetTitle(dataIndex);
            textMesh.text = title;

            if (visibleIndex == centerIndex)
                textMesh.color = Color.yellow;
            else
                textMesh.color = Color.white;
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
