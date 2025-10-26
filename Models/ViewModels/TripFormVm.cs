using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Jango_Travel.Models.ViewModels;

public class TripFormVm
{
    public int? Id { get; set; }

    [Required] public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    [Required] public DateTime StartDate { get; set; }
    [Required] public DateTime EndDate   { get; set; }

    // выбор существующего города
    public int? CityId { get; set; }

    // или создание нового
    public string? NewCityName { get; set; }
    public string? NewCountryName { get; set; }
    public double? NewCityLat { get; set; }
    public double? NewCityLon { get; set; }
    
    public string? DescriptionHtml { get; set; }

    // загрузка фото
    public List<IFormFile> Files { get; set; } = new();
}