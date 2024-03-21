using UnityEngine;

public static class MyInput
{
    private static CustomInput myInputs;

    public static void Init(Player p)
    {
        myInputs.Game.Move.performed += ctx =>
        {
            Vector2 t = ctx.ReadValue<Vector2>();
            Vector3 vec = new Vector3(t.x, 0, t.y);
            p.ReceiveMovement(vec);
        };
    }

    public static void InitializeBase()
    {
        myInputs = new CustomInput();
        //Do things...
        myInputs.Permanent.Enable();
        myInputs.Permanent.TickRate.performed += ctx =>
        {
            //NetworkManager.NetworkData.milliDelay = Mathf.Max(0, NetworkManager.MilliDelay + (int)ctx.ReadValue<float>());
            //Debug.Log("Setting the new milli delay: " + NetworkManager.MilliDelay);
        };
    }

public static void UIMode()
    {
        myInputs.Game.Enable();
        myInputs.UI.Disable();
    }

    public static void GameMode()
    {
        myInputs.Game.Enable();
        myInputs.UI.Disable();
    }

}
