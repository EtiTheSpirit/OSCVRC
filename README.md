# OSCVRC

A library designed to learn how OSC works. It works like any other OSC library with its own quirks.

It is designed for driving avatar parameters.

## Features
1. **Simple API.** Create a new OSC object (optionally with custom endpoints) and you are basically ready to go.
2. **Mnemonic Design.** You set an avatar parameter with the avidly named `SetAvatarParameter` method.
3. **Event-based Design.** The system can report parameter changes to you over C# events. It also has an event that fires when your avatar changes.