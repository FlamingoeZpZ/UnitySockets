using Netcode;
using TMPro;
using UnityEngine;

public class TextManager : MonoBehaviour
{
    
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private RectTransform bounds;
    [SerializeField] private TextMeshProUGUI prefab;
    [SerializeField] private Transform root;


    public static TextManager Instance { get; private set; }

    private Transform rootInstance;
    
    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        inputField.characterLimit = 100;
        rootInstance = Instantiate(root, bounds);
        inputField.onSubmit.AddListener(txt =>
        {
            string msg = NetworkManager.userName + ": " + txt;
            NetworkManager.Instance.TcpSendMessageToServer(0, msg);
            inputField.text = "";
            ReceiveMessageFromServer(msg);
        });
        Instance = this;
    }

    public void ReceiveMessageFromServer(string message)
    {
        TextMeshProUGUI t = Instantiate(prefab, rootInstance);
        t.text = message;
        //Increase bounds by some size.
        var delta = bounds.sizeDelta;
        float i = t.preferredHeight * 2;
        print(t.preferredHeight);
        t.rectTransform.sizeDelta = new Vector2(delta.x, i);
        delta.y += i;
        bounds.sizeDelta = delta;
    }

    public void ClearText()
    {
        Destroy(rootInstance);
        rootInstance = Instantiate(root, bounds);
        
        //I'm so lazy it's unreal.
        var delta = bounds.sizeDelta;
        delta.y = 0;
        bounds.sizeDelta = delta;
    }
}
