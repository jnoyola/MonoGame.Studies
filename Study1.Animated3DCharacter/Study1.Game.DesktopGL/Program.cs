using System.Reflection;
using Microsoft.Xna.Framework.Input;
using Study1.Game;

using var game = new Game();

// Pre-allocate memory.
var keyboardAssembly = typeof(Keyboard).Assembly;
var keyboardUtil = keyboardAssembly.GetType("Microsoft.Xna.Framework.Input.KeyboardUtil");
if (keyboardUtil != null)
{
    // Call the KeyboardUtil static constructor up front, instead of waiting until the first key is pressed.
    System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(keyboardUtil.TypeHandle);
}
var keysField = typeof(Keyboard).GetField("_keys", BindingFlags.NonPublic | BindingFlags.Static);
if (keysField != null)
{
    var keys = keysField.GetValue(null);
    if (keys is List<Keys> keyList)
    {
        // Make room for 8 keys to be pressed simultaneously, instead of expanding the list as keys are pressed.
        keyList.EnsureCapacity(8);
    }
}

game.Run();
