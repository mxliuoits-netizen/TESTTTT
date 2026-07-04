using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using PIC.Libary.Extentions;

namespace NotifyMonitorJob.Model.View;

public class ItemReflect<T> where T : new()
{
    public ItemReflect(T model, string propName)
    {
        Model = model;
        PropName = propName;
    }

    public T Model { get; }

    public string PropName { get; }

    public string DisplayName => Model.GetDisplayName(PropName);

    public PropertyInfo PropInfo => Model.GetType().GetProperty(PropName);

}