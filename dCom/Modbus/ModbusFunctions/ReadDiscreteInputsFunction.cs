using Common;
using Modbus.FunctionParameters;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;

namespace Modbus.ModbusFunctions
{
    /// <summary>
    /// Class containing logic for parsing and packing modbus read discrete inputs functions/requests.
    /// </summary>
    public class ReadDiscreteInputsFunction : ModbusFunction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReadDiscreteInputsFunction"/> class.
        /// </summary>
        /// <param name="commandParameters">The modbus command parameters.</param>
        public ReadDiscreteInputsFunction(ModbusCommandParameters commandParameters) : base(commandParameters)
        {
            CheckArguments(MethodBase.GetCurrentMethod(), typeof(ModbusReadCommandParameters));
        }

        /// <inheritdoc />
        public override byte[] PackRequest()
        {
            byte[] zahtev = new byte[12];

            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)CommandParameters.TransactionId)), 0, zahtev, 0, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)CommandParameters.ProtocolId)), 0, zahtev, 2, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)CommandParameters.Length)), 0, zahtev, 4, 2);
            zahtev[6] = CommandParameters.UnitId;
            zahtev[7] = CommandParameters.FunctionCode;
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)((ModbusReadCommandParameters)CommandParameters).StartAddress)), 0, zahtev, 8, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)((ModbusReadCommandParameters)CommandParameters).Quantity)), 0, zahtev, 10, 2);

            return zahtev;
        }

        /// <inheritdoc />
        public override Dictionary<Tuple<PointType, ushort>, ushort> ParseResponse(byte[] response)
        {
            var povratna = new Dictionary<Tuple<PointType, ushort>, ushort>();

            if (response[7] == CommandParameters.FunctionCode + 0x80)
            {
                HandeException(response[8]);
            }
            else
            {
                int brojac = 0;
                ushort adresa = ((ModbusReadCommandParameters)CommandParameters).StartAddress;
                ushort vrednost = 0;
                byte maska = 1;

                for (int i = 0; i < response[8]; i++)
                {
                    byte temp = response[9 + i];
                    for (int j = 0; j < 8; j++)
                    {
                        vrednost = (ushort)(temp & maska);
                        temp >>= 1;

                        povratna.Add(new Tuple<PointType, ushort>(PointType.DIGITAL_INPUT, adresa), vrednost);
                        brojac++;
                        adresa++;

                        if (brojac == ((ModbusReadCommandParameters)CommandParameters).Quantity)
                        {
                            break;
                        }
                    }
                }
            }

            return povratna;
        }
    }
}