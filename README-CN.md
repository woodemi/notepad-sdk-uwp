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