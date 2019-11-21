English | [简体中文](./README-CN.md)

# Usage
- Scan notepad
- Connect notepad
- Sync notepen pointer

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

## Sync notepen pointer
### NotePadClient#setMode
- NotepadMode.Common    
The device only saves NotePenPointer (with time stamp) of pressure >0 to offline handwriting
- NotepadMode.Sync  
The device sends all NotePenPointer (without timestamp) to the connected phone /Pad

The device defaults to NotepadMode.Common (connected/unconnected), and setMode only changes after the connection
```c#
NotepadClient notepadClient;
notepadClient.SetMode(NotepadMode.Sync);
```

### NotePadClient.SyncPointerReceived#handlePointer
When notepadmode.sync, receive NotePenPointer

```c#
notepadClient.SyncPointerReceived += OnSyncPointerReceived;
private void OnSyncPointerReceived(NotepadClient sender, List<NotePenPointer> args)
{
    foreach (var pointer in args)
        Debug.WriteLine($"OnSyncPointerReceived {pointer}");
}
```