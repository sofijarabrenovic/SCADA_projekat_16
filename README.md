# SCADA – Sliding Gate Control System

A SCADA application for monitoring and automated control of a sliding gate, built on top of the **Modbus TCP** protocol. The system periodically polls gate position and sensor data, applies EGU conversion and alarm logic, and automates gate movement with obstacle detection.

---

## Overview

The gate position (`L`) is tracked as an analog value representing the distance from wall W1. The operator can remotely open or close the gate via digital outputs. The system enforces safety limits (LowAlarm / HighAlarm), raises alarms, and handles obstacle detection automatically — pausing closing and reversing the gate if an obstacle is detected mid-movement.

---

## Project Structure

```
SCADA_projekat_16/
├── dCom/                          # Main WPF SCADA application (.NET/C#)
│   ├── dCom/
│   │   ├── Configuration/
│   │   │   ├── ConfigReader.cs        # Parses RtuCfg.txt
│   │   │   └── ConfigItem.cs          # Per-point config (address, limits, alarms...)
│   │   ├── ViewModel/
│   │   │   ├── MainViewModel.cs       # UI data binding
│   │   │   └── PointViewModels/       # AnalogOutput, DigitalOutput, DigitalInput VMs
│   │   ├── Converters/                # WPF value converters (alarm colors, visibility)
│   │   ├── MainWindow.xaml            # Main monitoring UI
│   │   ├── ControlWindow.xaml         # Manual command window
│   │   └── RtuCfg.txt                 # RTU/point configuration
│   ├── ProcessingModule/
│   │   ├── Acquisitor.cs              # Periodic polling thread
│   │   ├── ProcessingManager.cs       # Read/write command execution + EGU conversion
│   │   ├── AlarmProcessor.cs          # Analog (Hi/Lo) and digital (Abnormal) alarm logic
│   │   ├── AutomationManager.cs       # Gate automation thread (open/close/obstacle logic)
│   │   └── EGUConverter.cs            # Raw ↔ engineering units conversion
│   ├── Modbus/
│   │   ├── Connection/
│   │   │   ├── TCPConnection.cs       # Winsock TCP connection to simulator
│   │   │   └── FunctionExecutor.cs    # Sends Modbus frames, handles responses
│   │   ├── ModbusFunctions/
│   │   │   ├── ReadCoilsFunction.cs         # FC01
│   │   │   ├── ReadDiscreteInputsFunction.cs # FC02
│   │   │   ├── ReadHoldingRegistersFunction.cs # FC03
│   │   │   ├── WriteSingleCoilFunction.cs   # FC05
│   │   │   └── WriteSingleRegisterFunction.cs # FC06
│   │   └── FunctionFactory.cs
│   └── Common/                        # Shared interfaces and enums
└── MdbSim/
    └── MdbSim/
        ├── ModbusSim.exe              # Modbus RTU/TCP simulator
        └── RtuCfg.txt                 # Simulator-side configuration
```

---

## Modbus Point Map

| Signal           | Type             | Address | Default | Description                        |
|------------------|------------------|---------|---------|------------------------------------|
| L (gate position)| Analog Output    | 1000    | 400 cm  | Distance from wall W1              |
| S (obstacle)     | Digital Input    | 2000    | 0 (OFF) | Obstacle indicator between walls   |
| Open             | Digital Output   | 3000    | 0 (OFF) | Open button state                  |
| Close            | Digital Output   | 3001    | 0 (OFF) | Close button state                 |

**Alarm limits for L:**
- `LowAlarm` = 20 cm — gate risks falling out of track
- `HighAlarm` = 600 cm — gate blocks pedestrian passage

**EGU conversion formula:** `EGU_value = A × raw_value + B` (default: A=1, B=0)

---

## Communication Configuration

```
RTU Slave Address : 15
Transport         : TCP
Port              : 25252
```

Both `dCom` and `MdbSim` must use this configuration to establish a connection.

---

## Automation Logic

The `AutomationManager` runs a dedicated thread that enforces the following behavior:

```
Every cycle:
  Read: L (position), Open button, Close button, Obstacle sensor

  if L > HighAlarm  →  force Close = OFF
  if L < LowAlarm   →  force Open  = OFF

  if Open == ON     →  L -= 10 cm (step)
      if obstacle active and L <= LowAlarm  →  stop at LowAlarm, Open = OFF

  if Close == ON    →  L += 10 cm (step)
      if obstacle detected  →  Close = OFF, Open = ON (reverse to LowAlarm)
                               set preprekaAktivna = true

  if preprekaAktivna and obstacle cleared and Open == OFF
                    →  resume closing (Close = ON), preprekaAktivna = false

  if position changed  →  write new raw value to address 1000, sleep 1s
```

> Open and Close cannot be active simultaneously. The process cannot be manually stopped mid-movement — only alarm limits or the obstacle sensor can interrupt it.

---

## Alarm Types

| Alarm            | Trigger condition                                      |
|------------------|--------------------------------------------------------|
| `HIGH_ALARM`     | Gate position L > 600 cm                              |
| `LOW_ALARM`      | Gate position L < 20 cm                               |
| `ABNORMAL_VALUE` | Digital signal is in its abnormal state (non-nominal) |

Alarms are reflected visually in the UI via color-coded bindings (`AlarmToBackgroundColorConverter`).

---

## RtuCfg.txt Format

```
STA 15
TCP 25252

DO_REG  2 3000  0  0   1  0  DO  @DigOut  5  #  #  #  #  1  #  #
DI_REG  1 2000  0  0   1  0  DI  @DigIn   5  #  #  #  #  1  #  #
HR_INT  1 1000  0  0   1000  400  AO  @AnaOut  5  1  0  800  0  #  600  20
```

Fields per entry: `type  count  startAddr  ...  defaultValue  pointType  label  acquisitionInterval  A  B  highRaw  lowRaw  abnormalValue  highAlarm  lowAlarm`

---

## Requirements & Setup

- Windows
- .NET Framework 4.x / Visual Studio
- `ModbusSim.exe` must be running before starting `dCom`

**Steps to run:**
1. Launch `MdbSim/MdbSim/ModbusSim.exe` — simulator loads `RtuCfg.txt` automatically via `dConfig.txt`
2. Open `dCom/dCom.sln` in Visual Studio and run the project
3. The main window displays all configured points with live values and alarm indicators
4. Use the Control Window to issue manual Open/Close commands or update analog values
