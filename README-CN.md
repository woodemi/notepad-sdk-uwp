[English](./README.md) | 简体中文

# 功能
- 扫描设备

## 扫描设备

```c#
NotepadScanner notepadScanner = new NotepadScanner();
notepadScanner.Found += (sender, args) => Debug.WriteLine($"OnNotepadFound {args}");

notepadScanner.StartScan()
// ...
notepadScanner.StopScan()
```

## 连接设备

连接从`NotepadScanner.Found`中扫描到的`result`

```c#
NotepadConnector notepadConnector = new NotepadConnector();
notepadConnector.ConnectionChanged += (sender, args) => Debug.WriteLine($"OnConnectionChanged {sender} {args}");

notepadConnector.Connect(result);
// ...
notepadConnector.Disconnect();
```