using Netcode;
using TMPro;
using UnityEngine;

public class UIManagar : MonoBehaviour
{
    [SerializeField] private TMP_InputField name;
    [SerializeField] private TMP_InputField ip;
    [SerializeField] private TextMeshProUGUI failedObject;
    [SerializeField] private GameObject panelA;
    [SerializeField] private GameObject panelB;


    public async void TryConnect()
    {
        bool success = await NetworkManager.Instance.Initialize(name.text, ip.text);
        failedObject.gameObject.SetActive(!success);
        panelA.SetActive(!success);
        panelB.SetActive(success);
        print("Yippie");
    }

    public void Quit()
    {
        panelA.SetActive(true);
        panelB.SetActive(false); 
        NetworkManager.Instance.Quit();
    }

}
