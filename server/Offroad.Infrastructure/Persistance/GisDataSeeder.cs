using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Newtonsoft.Json; 
using Routing.Domain.Models;
using Routing.Infrastructure.Data; 

namespace Routing.Infrastructure.Persistance;

public class GisDataSeeder
{
    private readonly ApplicationDbContext _dbContext;

    public GisDataSeeder(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SeedZonesAsync(string geoJsonFilePath, ZoneType type, string defaultName)
    {
        if (await _dbContext.GeoZones.AnyAsync(z => z.Type == type))
            return;

        if (!File.Exists(geoJsonFilePath))
            throw new FileNotFoundException($"GeoJSON soubor nenalezen: {geoJsonFilePath}");

        var batchSize = 10000; 
        var batch = new List<GeoZone>();
        int totalSaved = 0;

        // 1. set serializer
        var serializer = GeoJsonSerializer.Create();

        // 2. create stream
        using var streamReader = new StreamReader(geoJsonFilePath);
        using var jsonReader = new JsonTextReader(streamReader);

        // 3. go through file and find features
        while (await jsonReader.ReadAsync())
        {
            if (jsonReader.TokenType == JsonToken.PropertyName && jsonReader.Value?.ToString() == "features")
            {
                await jsonReader.ReadAsync();

                // 4. go through the features one by one
                while (await jsonReader.ReadAsync() && jsonReader.TokenType != JsonToken.EndArray)
                {
                    if (jsonReader.TokenType == JsonToken.StartObject)
                    {
                        var feature = serializer.Deserialize<NetTopologySuite.Features.Feature>(jsonReader);

                        if (feature?.Geometry is Polygon polygon)
                        {
                            batch.Add(CreateZone(polygon, type, defaultName));
                        }
                        else if (feature?.Geometry is MultiPolygon multiPolygon)
                        {
                            foreach (Polygon poly in multiPolygon.Geometries)
                            {
                                batch.Add(CreateZone(poly, type, defaultName));
                            }
                        }

                        // 5. check batch size+ save & release
                        if (batch.Count >= batchSize)
                        {
                            await _dbContext.GeoZones.AddRangeAsync(batch);
                            await _dbContext.SaveChangesAsync();

                            
                            _dbContext.ChangeTracker.Clear();

                            totalSaved += batch.Count;
                            batch.Clear();
                        }
                    }
                }
            }
        }

        // save the rest(if any)
        if (batch.Any())
        {
            await _dbContext.GeoZones.AddRangeAsync(batch);
            await _dbContext.SaveChangesAsync();
            _dbContext.ChangeTracker.Clear();
            totalSaved += batch.Count;
        }
    }

    private GeoZone CreateZone(Polygon geometry, ZoneType type, string name)
    {
        geometry.SRID = 4326;// needed for postGIS, standart GPS coordinates
        return new GeoZone
        {
            Id = Guid.NewGuid(),
            Name = name,
            Type = type,
            Geometry = geometry
        };
    }
}