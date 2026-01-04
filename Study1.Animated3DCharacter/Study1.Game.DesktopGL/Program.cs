using Study1.Game;

using var game = new Game();

// Pre-allocate memory.
var keyboardAssembly = typeof(Microsoft.Xna.Framework.Input.Keyboard).Assembly;
var keyboardUtil = keyboardAssembly.GetType("Microsoft.Xna.Framework.Input.KeyboardUtil");
if (keyboardUtil != null)
{
    System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(keyboardUtil.TypeHandle);
}

game.Run();
