using System.Threading;
using System.Threading.Tasks;
using Atlas.Application.Amis.Models;

namespace Atlas.Application.Amis.Abstractions;

public interface IAmisSchemaProvider
{
    Task<AmisPageDefinition?> GetByKeyAsync(string key, CancellationToken cancellationToken);
}
