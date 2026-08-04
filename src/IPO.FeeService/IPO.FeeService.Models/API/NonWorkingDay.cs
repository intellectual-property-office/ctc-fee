using IPO.FeeService.Models.Constants;
using System;

namespace IPO.FeeService.Models.API
{
    public class NonWorkingDay
    {
        public NonWorkingDay(DateTime date, NonWorkingDayType type, string description) 
        { 
            Date = date;
            Type = type;
            Description = description;
        }

        public DateTime Date { get; set; }

        public NonWorkingDayType Type { get; set; }

        public string Description { get; set; }
    }
}
