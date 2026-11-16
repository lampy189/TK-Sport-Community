using System.ComponentModel.DataAnnotations;

namespace be_da.Application.DTOs;
public sealed record BookingContact(string Draft, IReadOnlyList<BookingLink> Links);
