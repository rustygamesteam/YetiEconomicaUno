using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;
using RustyDTO.Interfaces;

namespace YetiEconomicaUno.ViewModels;

public record ResourcesByGroup(IRustyEntity Group, IEnumerable<IRustyEntity> Children)
{
    public Symbol Icon => Symbol.Folder;
    public string DisplayName => Group.DisplayName;
}