using UnityEngine;
using TMPro;

public class ListController : MonoBehaviour
{
    [SerializeField] private TrackListDataSource _dataSource;
    [SerializeField] private int _visibleItems = 6;
    [SerializeField] private float _itemHeight = 0.5f;
    [SerializeField] private float _itemWidth = 2f;
    [SerializeField] private float _listDistance = 5f;

    public event System.Action<int> OnItemSelected;

    public void SetDataSource(TrackListDataSource dataSource)
    {
        _dataSource = dataSource;
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
    private bool _subscribed;

    void Start()
    {
        if (_dataSource != null)
        {
            _dataSource.OnDataChanged += RefreshItems;
        }

        if (XRInputManager.Instance != null)
        {
            XRInputManager.Instance.OnJoystickMoved += OnJoystickMoved;
            XRInputManager.Instance.OnAButtonPressed += OnAButtonPressed;
            _subscribed = true;
            Debug.Log("ListController subscribed to XRInputManager");
        }
        else
        {
            Debug.LogError("ListController: XRInputManager.Instance is null!");
        }
    }

    void Update()
    {
        if (XRInputManager.Instance != null && !_subscribed)
        {
            XRInputManager.Instance.OnJoystickMoved += OnJoystickMoved;
            XRInputManager.Instance.OnAButtonPressed += OnAButtonPressed;
            _subscribed = true;
            Debug.Log("ListController retried subscription in Update()");
        }
    }

    void CreateItems()
    {
        _items = new GameObject[_visibleItems];
        int count = _dataSource.Count;
        if (count == 0) return;

        for (int i = 0; i < _visibleItems; i++)
        {
            GameObject item = new GameObject($"ListItem_{i}");
            item.transform.SetParent(transform);

            MeshFilter meshFilter = item.AddComponent<MeshFilter>();
            MeshCollider meshCollider = item.AddComponent<MeshCollider>();
            MeshRenderer meshRenderer = item.AddComponent<MeshRenderer>();

            meshFilter.mesh = CreateQuadMesh();
            meshRenderer.material = new Material(Shader.Find("Standard"));
            meshCollider.convex = true;

            GameObject textObj = new GameObject("TextLabel");
            textObj.transform.SetParent(item.transform);
            textObj.transform.localPosition = new Vector3(0, 0, 0.6f);

            TextMeshPro textMesh = textObj.AddComponent<TextMeshPro>();
            textMesh.text = _dataSource.GetTitle((i + _offset) % count);
            textMesh.fontSize = 18;
            textMesh.alignment = TextAlignmentOptions.Center;

            _items[i] = item;
            UpdateItemPosition(i);
        }
    }

    void UpdateItemPosition(int visibleIndex)
    {
        if (_dataSource.Count == 0 || _items == null || _items[visibleIndex] == null) return;
        int dataIndex = (_offset + visibleIndex) % _dataSource.Count;
        int centerIndex = _visibleItems / 2;

        float yOffset = (centerIndex - visibleIndex) * _itemHeight;
        _items[visibleIndex].transform.localPosition = new Vector3(0, yOffset, _listDistance);

        Camera cam = Camera.main;
        if (cam != null)
        {
            Vector3 dirToCamera = cam.transform.position - _items[visibleIndex].transform.position;
            _items[visibleIndex].transform.rotation = Quaternion.LookRotation(dirToCamera) * Quaternion.Euler(0, 180, 0);
        }

        TextMeshPro textMesh = _items[visibleIndex].GetComponentInChildren<TextMeshPro>();
        if (textMesh != null)
        {
            string title = _dataSource.GetTitle(dataIndex);
            textMesh.text = title;

            if (visibleIndex == centerIndex)
                textMesh.color = Color.yellow;
            else
                textMesh.color = Color.white;
        }
    }

    void RefreshItems()
    {
        Debug.Log($"ListController.RefreshItems called, _items={_items == null}, count={_dataSource.Count}");
        if (_items == null || _dataSource.Count == 0) CreateItems();
        else
        {
            for (int i = 0; i < _visibleItems; i++)
            {
                UpdateItemPosition(i);
            }
        }
    }

    void OnJoystickMoved(Vector2 value)
    {
        if (!gameObject.activeSelf) return;
        if (_dataSource == null)
        {
            Debug.LogError("ListController._dataSource is null");
            return;
        }

        int count = _dataSource.Count;
        if (count == 0) return;

        int direction = 0;
        if (value.y < -0.5f) direction = 1;
        else if (value.y > 0.5f) direction = -1;

        if (direction != 0)
        {
            _offset = (_offset + direction + count) % count;
            RefreshItems();
        }
    }

    void OnAButtonPressed()
    {
        if (!gameObject.activeSelf) return;
        if (_dataSource.Count == 0) return;

        int centerIndex = _visibleItems / 2;
        int selectedIndex = (_offset + centerIndex) % _dataSource.Count;
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

        if (_dataSource != null)
        {
            _dataSource.OnDataChanged -= RefreshItems;
        }
    }
}
