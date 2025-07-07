using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using NModbus;
using NModbus.Device;
namespace DirectionDetection.PLC
{


  
    //public class ModbusTcpClient : IDisposable
    //{
    //    private TcpClient tcpClient;
    
    //    private ModbusFactory modbusFactory;
    //    private IModbusMaster master;

    //    public string IpAddress { get; private set; }
    //    public int Port { get; private set; }
    //    public byte SlaveId { get; private set; }

    //    public ModbusTcpClient(string ipAddress, int port = 502, byte slaveId = 1)
    //    {
    //        IpAddress = ipAddress;
    //        Port = port;
    //        SlaveId = slaveId;
    //    }

    //    public bool Connect()
    //    {
    //        try
    //        {
    //            tcpClient = new TcpClient();
    //            tcpClient.Connect(IpAddress, Port);
    //            modbusFactory = new ModbusFactory();
    //            master = modbusFactory.CreateMaster(tcpClient);
    //            master.Transport.ReadTimeout = 1000;
    //            return true;
    //        }
    //        catch (Exception ex)
    //        {
    //            Console.WriteLine("Modbus connection failed: " + ex.Message);
    //            return false;
    //        }
    //    }

    //    public bool[] ReadCoils(ushort startAddress, ushort num)
    //    {
    //        return master.ReadCoils(SlaveId, startAddress, num);
    //    }

    //    public bool[] ReadDiscreteInputs(ushort startAddress, ushort num)
    //    {
    //        return master.ReadInputs(SlaveId, startAddress, num);
    //    }

    //    public ushort[] ReadHoldingRegisters(ushort startAddress, ushort num)
    //    {
    //        return master.ReadHoldingRegisters(SlaveId, startAddress, num);
    //    }

    //    public ushort[] ReadInputRegisters(ushort startAddress, ushort num)
    //    {
    //        return master.ReadInputRegisters(SlaveId, startAddress, num);
    //    }

    //    public void WriteSingleCoil(ushort address, bool value)
    //    {
    //        master.WriteSingleCoil(SlaveId, address, value);
    //    }

    //    public void WriteMultipleCoils(ushort startAddress, bool[] values)
    //    {
    //        master.WriteMultipleCoils(SlaveId, startAddress, values);
    //    }

    //    public void WriteSingleRegister(ushort address, ushort value)
    //    {
    //        master.WriteSingleRegister(SlaveId, address, value);
    //    }

    //    public void WriteMultipleRegisters(ushort startAddress, ushort[] values)
    //    {
    //        master.WriteMultipleRegisters(SlaveId, startAddress, values);
    //    }

    //    public void Disconnect()
    //    {
    //        master?.Dispose();
    //        tcpClient?.Close();
    //    }

    //    public void Dispose()
    //    {
    //        Disconnect();
    //    }
    //}


}
