using Recuria.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Domain
{
    public class Plan
    {
        public PlanType Type { get; private set; }
        public int MaxUsers { get; private set; }
        public decimal MonthlyPrice { get; private set; }

        public Plan(PlanType type, int maxUsers, decimal monthly)
        {
            Type = type;
            MaxUsers = maxUsers;
            MonthlyPrice = monthly;
        }
    }
}
