using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

var app = builder.Build();

app.UseCors("AllowAllOrigins");

List<Order> repo = new List<Order>
{
    new Order(1, new DateTime(2024, 6, 26), "Телефон","сломался", "Работает","Полюс","+79222971782","0")
    
    
};

bool isUpdatedStatus = false;
string message = "";

app.MapGet("/", () =>
{
    if (isUpdatedStatus)
    {
        string buffer = message;
        isUpdatedStatus = false;
        message = "";
        return Results.Json(new OrderUpdateStatusDTO(repo, buffer));
    }
    return Results.Json(repo);
});

app.MapPost("/", (Order o) =>
{
    repo.Add(o);
    return Results.Created($"/{o.Numder}", o);
});

app.MapPut("/{num}", (int num, OrderUpdateDTO dto) =>
{
    Order buffer = repo.Find(o => o.Numder == num);
    if (buffer == null)
        return Results.StatusCode(404);

    if (buffer.Description != dto.Description)
        buffer.Description = dto.Description;

    if (buffer.Master != dto.Master)
        buffer.Master = dto.Master;

    if (buffer.Status != dto.Status)
    {
        buffer.Status = dto.Status;
        isUpdatedStatus = true;
       
    }

    if (dto.Comment != null && dto.Comment != "")
        buffer.Comment.Add(dto.Comment);

    return Results.Json(buffer);
});

app.MapGet("/{num}", (int num) => repo.Find(o => o.Numder == num));

app.MapGet("/filter/{param}", (string param) => repo.FindAll(o =>
    o.Device == param ||
    o.ProblemType == param ||
    o.Description == param ||
    o.Client == param ||
    o.Status == param ||
    o.Master == param));

app.MapGet("/statistics", () =>
{
    var completedOrders = repo.Count(o => o.Status == "");
    var averageCompletionTime = repo.Where(o => o.Status == "").Average(o => (o.DateAdded - DateTime.Now).TotalDays);
    var problemTypeStats = repo.GroupBy(o => o.ProblemType).Select(g => new { ProblemType = g.Key, Count = g.Count() });

    return Results.Json(new
    {
        CompletedOrders = completedOrders,
        AverageCompletionTime = averageCompletionTime,
        ProblemTypeStats = problemTypeStats
    });
});

app.Run();

class OrderUpdateDTO
{
    public string Status { get; set; }
    public string Description { get; set; }
    public string Master { get; set; }
    public string Comment { get; set; }
    public string Phone { get; set; }
}

class OrderUpdateStatusDTO
{
    public List<Order> Orders { get; set; }
    public string Message { get; set; }

    public OrderUpdateStatusDTO(List<Order> orders, string message)
    {
        Orders = orders;
        Message = message;
    }
}

class Order
{
    public int Numder { get; set; }
    public DateTime DateAdded { get; set; }
    public string Device { get; set; }
    public string ProblemType { get; set; }
    public string Description { get; set; }
    public string Client { get; set; }
    public string Status { get; set; }
    public string Master { get; set; }
    public string Phone { get; set; }
    public List<string> Comment { get; set; } = new List<string>();

    public Order(int numder, DateTime dateAdded, string device, string problemType, string description, string client, string phone, string status)
    {
        Numder = numder;
        DateAdded = dateAdded;
        Device = device;
        ProblemType = problemType;
        Description = description;
        Client = client;
        Status = status;
        Phone = phone;
        Master = "Босс";
    }
}