using System.ComponentModel.DataAnnotations;

namespace AutoServiceApi.Models;

public class CreateWorkOrderRequest
{
    [Required(ErrorMessage = "Укажите государственный номер автомобиля")]
    [StringLength(15, MinimumLength = 5,
        ErrorMessage = "Госномер — от 5 до 15 символов")]
    public string CarNumber { get; set; } = "";
    [Required(ErrorMessage = "Укажите ФИО клиента")]
    [StringLength(100, MinimumLength = 3,
        ErrorMessage = "ФИО клиента — от 3 до 100 символов")]
    public string ClientName { get; set; } = "";
    [Required(ErrorMessage = "Укажите описание работ")]
    [StringLength(500, MinimumLength = 10,
        ErrorMessage = "Описание — от 10 до 500 символов")]
    public string Description { get; set; } = "";
    [Required(ErrorMessage = "Укажите стоимость работ")]
    [Range(0, 1000000,
        ErrorMessage = "Стоимость должна быть от 0 до 1 000 000 руб.")]
    public decimal? Cost { get; set; }
}