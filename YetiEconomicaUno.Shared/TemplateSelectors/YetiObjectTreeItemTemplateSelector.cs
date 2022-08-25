using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RustyDTO.Interfaces;

namespace YetiEconomicaUno.TemplateSelectors;

public class YetiObjectTreeItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate GroupTemplate { get; set; }
    public DataTemplate YetiObjectTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        if (item is null)
            return null;

        if (item is IRustyEntity)
            return YetiObjectTemplate;
        return GroupTemplate;
    }
}