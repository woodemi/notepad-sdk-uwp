English | [简体中文](./README-CN.md)

# Usage
- Scan notepad

## Scan notepad

```c#
NotepadScanner notepadScanner = new NotepadScanner();
notepadScanner.Found += (sender, args) => Debug.WriteLine($"OnNotepadFound {args}");

notepadScanner.StartScan()
// ...
notepadScanner.StopScan()
```

## Connect notepad

Connect to `result`, received from `NotepadScanner.Found`

```c#
NotepadConnector notepadConnector = new NotepadConnector();
notepadConnector.ConnectionChanged += (sender, args) => Debug.WriteLine($"OnConnectionChanged {sender} {args}");

notepadConnector.Connect(result);
// ...
notepadConnector.Disconnect();
```