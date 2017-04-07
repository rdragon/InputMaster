# InputMaster
This program lets you change the functionality of each key on your keyboard. For example, you can turn a non-modifier key into a modifier key and vice versa. You can also create keys similar to [compose keys](https://en.wikipedia.org/wiki/Compose_key), which you can use to define key sequences that trigger arbitrary actions.

InputMaster also makes all modifier keys [sticky](https://en.wikipedia.org/wiki/Sticky_keys), which removes the need to press multiple keys simultaneously (but you still can). To make it better suited for general use, the behavior is a bit different than the default Sticky Keys behavior supported by the operating system.

## Requirements
- Windows 7/8/10
- .NET Framework 4.5.2
- Visual Studio 2015

## Build process
- Copy `Config.sample.cs` to `Config.cs`.
- Open `InputMaster.sln` in Visual Studio.
- Build.

To be able to use InputMaster it must be running in the background. Only one instance of InputMaster can be active at a time. Creating the `Release` build automatically closes the running InputMaster instance and starts the new one.

## Hotkey examples
In the following examples InputMaster is configured such that the Caps Lock key has become a modifier key (like Ctrl). The custom modifier is named `{Caps}`.

First we create a hotkey to simulate the original Caps Lock key (to toggle ALL CAPS on/off). We choose to trigger the hotkey by pressing Space Bar while our custom modifier is active:

```
{Caps}{Space}  Send {CapsLock}
```

Next we create a hotkey to quickly open our hotkeys file (where we define these hotkeys). We choose to trigger it by pressing the H key while our custom modifier is active:

```
{Caps}h  Run C:\Users\<UserName>\AppData\Roaming\InputMaster\Hotkeys.txt
```

Finally we create hotkeys to open a webpage, insert a special character, and insert multiple characters:

```
{Caps}d  Run http://www.dictionary.com/
{Caps}s  Send √
{Caps};  Send This message is inserted when the ";" key is pressed while Caps is active.
```

More examples can be found in `Resources/Tests.im`.

## Key sequences
In the previous section we created hotkeys that required you to press a modifier key and another key. Using different modifier keys and combinations of modifiers it is possible to create a large number of hotkeys. However, these can be hard to remember.

[Compose keys](https://en.wikipedia.org/wiki/Compose_key) enable you to define easy to remember key sequences to insert special characters. InputMaster has a similar feature, but does not limit you to only insert special characters.

The following example defines four key sequences to open four different web pages. All sequences start with the Right Windows Key. The first sequence enables you to press the keys Right Windows Key, T, E, D to start translating from English to Dutch.

```
{RWin}  EnterMode MyMode
√> ComposeMode MyMode
  ted  Run http://translate.google.nl/#en/nl
  tde  Run http://translate.google.nl/#nl/en
  tes  Run http://translate.google.nl/#en/es
  tse  Run http://translate.google.nl/#es/en
```

## Sticky Keys
Windows comes with an accessibility feature called [Sticky Keys](https://en.wikipedia.org/wiki/Sticky_keys) (Linux and OS X have a similar feature) which enables you to press and release one or more modifier keys (Shift, Ctrl, Alt, Windows) and then press a non-modifier key to make the chosen modifiers be active during the latter key press. More precisely, pressing a modifier key has the effect of making the modifier become virtually "stuck" (or "unstuck", if it was already stuck). Then, when a non-modifier key is pressed any stuck modifiers are applied to this key press and become unstuck in the process.

This program mimics the Sticky Keys feature found in Windows, but with one difference. Pressing a modifier key cannot unstuck the modifier. When it was already stuck, it simply stays that way. This solves the problem of accidentally hitting a modifier key twice. It also makes it easier to keep track of which modifiers are stuck. To release all stuck modifiers you can press Escape.

Because this feature allows you to never having to hold down a key while typing or pressing hotkeys (but you still can), it makes the keyboard easier and faster to use.

## Limitations
- Works only on Windows.
- Needs to be run as administrator, or it will not function when a program with administrator rights has the foreground.
- Due to the way modifier keys are handled some applications that listen to modifier key presses will not function correctly.
- The program expects a single fixed keyboard layout. Switching between keyboard layouts is not supported.
- Has only been tested with the standard US keyboard layout that comes with Windows together with an ISO 105-key keyboard. There is no support for other keyboard layouts, but adding an extra layout should not be too hard.

## Technical information
Behind the scenes a [low level keyboard hook](https://msdn.microsoft.com/en-us/library/windows/desktop/ms644985(v=vs.85).aspx) and a [low level mouse hook](https://msdn.microsoft.com/en-us/library/windows/desktop/ms644986(v=vs.85).aspx) are used to register and capture user input. The simulation of user input is done with the [SendInput function](https://msdn.microsoft.com/nl-nl/library/windows/desktop/ms646310(v=vs.85).aspx).

### The handling of modifier keys
All modifier key presses are captured by InputMaster. Their events will not reach the foreground window. Instead, InputMaster keeps track of which modifiers are active. When a non-modifier key is pressed and (standard) modifiers are active, the non-modifier key down is captured and a sequence of key events is injected into the input stream. This sequence consists of the active (standard) modifier key downs, the non-modifier key down that was captured, and finally the active (standard) modifier key ups.

Handling the modifiers this way allows us to call SendInput without having to worry about which modifiers are currently active. This is because as far as Windows and the foreground window are concerned, no modifiers are active (these have all been captured). Another benefit of capturing the modifiers is that the foreground window will never receive a key down of a modifier directly followed by a key up of the same modifier (except when we simulate such a key press). This would be problematic for the Windows key, as it would open the start menu, and for the Alt key, as it would focus the foreground window menu.

The downside is that some applications that listen to modifier key presses will no longer function correctly. As of yet this has not given any real problems.
