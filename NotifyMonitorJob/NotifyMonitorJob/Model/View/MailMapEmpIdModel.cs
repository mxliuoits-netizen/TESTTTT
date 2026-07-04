using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotifyMonitorJob.Model.View;

public class MailMapEmpIdModel
{

    public string EmpId { get; set; }

    public string EmpName { get; set; }

    public string Email { get; set; }


    public string MailToName {
        get
        {
            var result = EmpName + $"({EmpId})";
            return result;
        }
    }
}
