/*
 * Copyright 2012 - Adam Haile
 * http://adamhaile.net
 *
 * This file is part of PandoraSharp.
 * PandoraSharp is free software: you can redistribute it and/or modify 
 * it under the terms of the GNU General Public License as published by 
 * the Free Software Foundation, either version 3 of the License, or 
 * (at your option) any later version.
 * 
 * PandoraSharp is distributed in the hope that it will be useful, 
 * but WITHOUT ANY WARRANTY; without even the implied warranty of 
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the 
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License 
 * along with PandoraSharp. If not, see http://www.gnu.org/licenses/.
*/

namespace Elpis.PandoraSharp
{
    public class PandoraException : System.Exception
    {
        public PandoraException(Util.ErrorCodes fault, System.Exception innerException)
            : base(Util.Errors.GetErrorMessage(fault), innerException)
        {
            Fault = fault;
        }

        public PandoraException(Util.ErrorCodes fault) : base(Util.Errors.GetErrorMessage(fault))
        {
            Fault = fault;
        }

        public Util.ErrorCodes Fault { get; set; }

        public string FaultMessage => Util.Errors.GetErrorMessage(Fault);
    }

    public class XmlRpcException : System.Exception
    {
        public XmlRpcException(string msg, System.Exception innerException) : base(msg, innerException) {}

        public XmlRpcException(string msg) : base(msg) {}
    }
}