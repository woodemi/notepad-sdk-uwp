[English](./README.md) | 简体中文

# 功能
- 扫描设备
- 连接设备
- 接收实时笔迹

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

## 接收实时笔迹
### NotePadClient#setMode
- NotepadMode.Common    
设备仅保存压力>0的NotePenPointer（含时间戳）到离线字迹中
- NotepadMode.Sync  
设备发送所有NotePenPointer（无时间戳）到连接的手机/Pad上

设备默认为NotepadMode.Common（连接/未连接），只有连接后setMode才会更改

```c#
NotepadClient notepadClient;
notepadClient.SetMode(NotepadMode.Sync);
```
### NotePadClient.SyncPointerReceived#handlePointer
当NotepadMode.Sync时，接收NotePenPointer

```c#
notepadClient.SyncPointerReceived += OnSyncPointerReceived;
private void OnSyncPointerReceived(NotepadClient sender, List<NotePenPointer> args)
{
    foreach (var pointer in args)
        Debug.WriteLine($"OnSyncPointerReceived {pointer}");
}
```