English | [简体中文](./README-CN.md)

# Usage
- Scan notepad

## Scan notepad

```c#
NotepadConnector notepadConnector = new NotepadConnector();
notepadScanner.Found += (sender, args) => Debug.WriteLine($"OnNotepadFound {args}");

notepadScanner.StartScan()
// ...
notepadScanner.StopScan()
```