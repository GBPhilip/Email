using System;

namespace Routeco.EmailWorkService.Domain
{
    public class EmailRequest
    {
        public int Id { get; set; }
        public string Message { get; set; }

        public DateTime TimeStamp { get; set; }
    }
}
