/*
 * Copyright 2012 - Adam Haile
 * http://adamhaile.net
 *
 * This file is part of Elpis.
 * Elpis is free software: you can redistribute it and/or modify 
 * it under the terms of the GNU General Public License as published by 
 * the Free Software Foundation, either version 3 of the License, or 
 * (at your option) any later version.
 * 
 * Elpis is distributed in the hope that it will be useful, 
 * but WITHOUT ANY WARRANTY; without even the implied warranty of 
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the 
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License 
 * along with Elpis. If not, see http://www.gnu.org/licenses/.
*/

namespace Util
{
    public class HashHelper
    {
        public static string HashArrayToString(byte[] hash)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (byte b in hash) sb.Append(b.ToString("X2"));
            return sb.ToString();
        }

        public static string GetStringHash(string data)
        {
            byte[] hash =
                new System.Security.Cryptography.MD5CryptoServiceProvider().ComputeHash(
                    System.Text.Encoding.ASCII.GetBytes(data));
            return HashArrayToString(hash);
        }
    }

    public class SystemInfo
    {
        public static string GetUniqueHash()
        {
            System.Management.ManagementObjectCollection cpus =
                new System.Management.ManagementObjectSearcher("SELECT * FROM Win32_Processor").Get();
            string cpuSerial = "";
            foreach (System.Management.ManagementBaseObject c in cpus)
            {
                cpuSerial = (string) c["ProcessorId"];
                break;
            }

            System.Management.ManagementObjectCollection mobos =
                new System.Management.ManagementObjectSearcher("SELECT * FROM Win32_BaseBoard").Get();
            string mobo = "";
            foreach (System.Management.ManagementBaseObject m in mobos)
            {
                mobo = (string) m["SerialNumber"];
                break;
            }

            return (cpuSerial + mobo).Md5Hash();
        }

        public static string GetWindowsVersion()
        {
            System.OperatingSystem os = System.Environment.OSVersion;
            System.Version vs = os.Version;

            string operatingSystem = "";

            if (os.Platform != System.PlatformID.Win32Windows)
            {
                if (os.Platform == System.PlatformID.Win32NT)
                {
                    switch (vs.Major)
                    {
                        case 3:
                            operatingSystem = "NT 3.51";
                            break;
                        case 4:
                            operatingSystem = "NT 4.0";
                            break;
                        case 5:
                            operatingSystem = vs.Minor == 0 ? "2000" : "XP";
                            break;
                        case 6:
                            switch (vs.Minor)
                            {
                                case 0:
                                    operatingSystem = "Vista";
                                    break;
                                case 1:
                                    operatingSystem = "7";
                                    break;
                                case 2:
                                    operatingSystem = "8";
                                    break;
                                default:
                                    operatingSystem = "Future";
                                    break;
                            }
                            break;
                        default:
                            operatingSystem = "Future";
                            break;
                    }
                }
            }
            else
            {
                switch (vs.Minor)
                {
                    case 0:
                        operatingSystem = "95";
                        break;
                    case 10:
                        operatingSystem = vs.Revision.ToString() == "2222A" ? "98SE" : "98";
                        break;
                    case 90:
                        operatingSystem = "Me";
                        break;
                }
            }
            if (operatingSystem == "") return operatingSystem;
            operatingSystem = "Windows " + operatingSystem;

            if (os.ServicePack != "")
            {
                operatingSystem += " " + os.ServicePack;
            }

            string pa = System.Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE",
                System.EnvironmentVariableTarget.Machine);
            int osArch = string.IsNullOrEmpty(pa) || string.Compare(pa, 0, "x86", 0, 3, true) == 0 ? 32 : 64;

            operatingSystem += " " + osArch + "-bit";

            return operatingSystem;
        }
    }
}