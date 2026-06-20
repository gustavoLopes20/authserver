using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AuthServer.Infra;

public class StringListConverter : ValueConverter<List<string>, string>
{
    public StringListConverter() : base(v => string.Join(',', v), v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList())
    {
    }
}