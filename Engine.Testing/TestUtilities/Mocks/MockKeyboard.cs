using Silk.NET.Input;

namespace Engine.Testing.TestUtilities.Mocks;

public class MockKeyboard : IKeyboard
{
    public string Name { get; } = "MockKeyboard";
    public int Index { get; } = 0;
    public bool IsConnected { get; } = true;
    public bool IsKeyPressed(Key key)
    {
        throw new NotImplementedException();
    }

    public bool IsScancodePressed(int scancode)
    {
        throw new NotImplementedException();
    }

    public void BeginInput()
    {
        throw new NotImplementedException();
    }

    public void EndInput()
    {
        throw new NotImplementedException();
    }

    public IReadOnlyList<Key> SupportedKeys { get; } = [];
    public string ClipboardText { get; set; } = "";
    public event Action<IKeyboard, Key, int>? KeyDown;
    public event Action<IKeyboard, Key, int>? KeyUp;
    public event Action<IKeyboard, char>? KeyChar;
    
    public void SendKeyDown(Key key, int scanCode = 0)
    {
        KeyDown?.Invoke(this, key, scanCode);
    }
    public void SendKeyUp(Key key, int scanCode = 0)
    {
        KeyUp?.Invoke(this, key, scanCode);
    }
    public void SendKeyChar(char character)
    {
        KeyChar?.Invoke(this, character);
    }
}