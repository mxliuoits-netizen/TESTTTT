using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UpdateMasterData.Model;

public class MailTemplateMModel
{
    public Int32 Templateno { get; set; }
    public string? Templatename { get; set; }
    public string Templatesubject { get; set; }
    public string Templatecontent { get; set; }
    public bool? Isactive { get; set; }
}
