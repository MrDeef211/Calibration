using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Model.Reports
{
    public class Identification
    {
        public DateTimeOffset TimeStamp { get; init; }
        public string Model { get; init; }
        public string SerialNumber { get; init; }
        public string SoftwareVersion { get; init; }
        public string StandId { get; init; }
        public string User {  get; init; }

        public Identification(
            DateTimeOffset? timeStamp,
            string model,
            string serialNumber,
            string softwareVersion,
            string standId,
            string user)
        {            
            TimeStamp = timeStamp ?? DateTimeOffset.Now;
            Model = model;
            SerialNumber = serialNumber;
            SoftwareVersion = softwareVersion;
            StandId = standId;
            User = user;
        }


    }
}
