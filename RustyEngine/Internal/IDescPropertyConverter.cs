using System.Text.Json;
using RustyDTO;

namespace RustyEngine.Internal;
internal interface IDescPropertyConverter
{
    public LazyDescProperty Resolve(JsonElement raw);
}