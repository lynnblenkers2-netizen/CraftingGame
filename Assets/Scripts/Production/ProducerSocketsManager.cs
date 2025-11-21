using UnityEngine;
using TMPro;

// Manages a small set of item slot UIs as producer sockets and adds spirit to the ResourceStore
public class ProducerSocketsManager : MonoBehaviour
{
    [Header("Sockets")]
    public ItemSlotUI[] sockets = new ItemSlotUI[3];

    [Header("Wiring")]
    public ResourceStore store;
    public ResourceDefinition spiritResource;
    public TextMeshProUGUI totalSpiritLabel;

    [Header("Settings")]
    public int socketCount = 3;

    Inventory socketsInventory;

    float spiritAccumulator = 0f;
    float lastTotalSpiritPerSecond = 0f;

    void Awake()
    {
        // try to auto-wire store/spirit if missing
        if (store == null)
            store = FindObjectOfType<ResourceStore>();

        if (spiritResource == null && store != null)
        {
            // try find a resource with id 'spirit'
            foreach (var e in store.resources)
            {
                if (e != null && e.def != null && e.def.id == "spirit") { spiritResource = e.def; break; }
            }
        }

        InitializeSocketsIfNeeded();
    }

    void InitializeSocketsIfNeeded()
    {
        // create inventory for sockets
        socketCount = Mathf.Max(1, socketCount);
        socketsInventory = new Inventory(socketCount);

        // If sockets array is larger/shorter than socketCount, clamp/allocate
        if (sockets == null || sockets.Length < socketCount)
        {
            var tmp = new ItemSlotUI[socketCount];
            if (sockets != null)
                for (int i = 0; i < Mathf.Min(sockets.Length, tmp.Length); i++) tmp[i] = sockets[i];
            sockets = tmp;
        }

        for (int i = 0; i < socketCount; i++)
        {
            if (sockets[i] != null)
            {
                sockets[i].Init(socketsInventory, i, ItemSlotUI.OwnerType.Inventory);
            }
        }

        if (socketsInventory != null) socketsInventory.OnChanged += OnSocketsChanged;
        RecomputeTotal();
    }

    void OnDestroy()
    {
        if (socketsInventory != null) socketsInventory.OnChanged -= OnSocketsChanged;
    }

    void OnSocketsChanged()
    {
        RecomputeTotal();
    }

    void RecomputeTotal()
    {
        float total = 0f;
        if (socketsInventory != null)
        {
            for (int i = 0; i < socketsInventory.Slots.Count; i++)
            {
                var st = socketsInventory.GetSlot(i);
                if (st == null || st.IsEmpty || st.Item == null) continue;
                var prod = st.Item.producer;
                if (prod != null)
                {
                    total += prod.spiritPerSecond * st.Amount;
                }
                else
                {
                    // nothing to add
                }
            }
        }
        lastTotalSpiritPerSecond = total;
        UpdateLabel();
    }

    void UpdateLabel()
    {
        if (totalSpiritLabel == null) return;
        totalSpiritLabel.text = string.Format("{0} /s", lastTotalSpiritPerSecond.ToString("F1"));
    }

    void Update()
    {
        // ensure wiring still valid
        if (store == null)
        {
            store = FindObjectOfType<ResourceStore>();
            if (store != null && spiritResource == null)
            {
                foreach (var e in store.resources)
                {
                    if (e != null && e.def != null && e.def.id == "spirit") { spiritResource = e.def; break; }
                }
            }
        }

        if (lastTotalSpiritPerSecond <= 0f || store == null || spiritResource == null) return;
        spiritAccumulator += lastTotalSpiritPerSecond * Time.deltaTime;
        if (spiritAccumulator >= 1f)
        {
            int toAdd = Mathf.FloorToInt(spiritAccumulator);
            spiritAccumulator -= toAdd;
            store.Add(spiritResource, toAdd);
        }
    }

    // Allow TopBarUI (or others) to provide sockets at runtime after instantiation
    public void AssignSockets(ItemSlotUI[] socketArray, int socketCountOverride = -1)
    {
        if (socketArray == null) return;
        sockets = socketArray;
        if (socketCountOverride > 0) socketCount = socketCountOverride;
        // Reinitialize using new sockets
        if (socketsInventory != null) socketsInventory.OnChanged -= OnSocketsChanged;
        InitializeSocketsIfNeeded();
    }
}
