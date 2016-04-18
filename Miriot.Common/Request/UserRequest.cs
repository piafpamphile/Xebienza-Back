using System;

namespace Miriot.Common.Request
{
    public class UserRequest
    {
        public Guid? Id { get; set; }
        public string Name { get; set; }
        public byte[] Image { get; set; }
        public string Widgets { get; set; }
    }
}
