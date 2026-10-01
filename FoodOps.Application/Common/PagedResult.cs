using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoodOps.Application.Common;

public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);