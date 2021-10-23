# Unity-Basic_Arduino_Connection
A basic serial port communication coding-example to communicate between arduino and Unity 3D.

## Import

1. Go to [release](https://github.com/ertanturan/Unity-Basic_Arduino_Connection/releases) page.
2. Download the lates release of the package.
3. Import it to your unity project.



# Setup

1. Atach `SerialPortCommunication.cs` to a gameobject in your scene.
2. Configure the Port and baudrate according to your arduino.
3. Press play and you should see the values that are being sent by the arduino through the Serial Port.

![Serial Port Communication](/GithubImages/SerialPortCommunication.png)


## Coding Interface

### Sending Data Through the Serial Port

``` csharp 
[SerializeField] // only if accessor is private
private SerialPortCommunication serialPortCommunication ;

serialPortCommunication.SendCustomData("EXAMPLE DATA")

```

### Receiving Data Coming Through the Serial Port

``` csharp 
//For more detail check `SerialPortCommunication.cs`  Update Function
    private SerialPortController c;
    .
    .
    .
    
 void Update ()
    {
        string receivedData;
        
        while (c.GetReceivedString(out receivedData))
        {
            Debug.Log("Received Data : " + receivedData);
        }
     }

```
