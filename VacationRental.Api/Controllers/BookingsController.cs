using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using VacationRental.Api.Models;

namespace VacationRental.Api.Controllers
{
    [Route("api/v1/bookings")]
    [ApiController]
    public class BookingsController : ControllerBase
    {
        private readonly IDictionary<int, RentalViewModel> _rentals;
        private readonly IDictionary<int, BookingViewModel> _bookings;

        public BookingsController(
            IDictionary<int, RentalViewModel> rentals,
            IDictionary<int, BookingViewModel> bookings)
        {
            _rentals = rentals;
            _bookings = bookings;
        }

        [HttpGet]
        [Route("{bookingId:int}")]
        public IActionResult Get(int bookingId)
        {
            if (!_bookings.ContainsKey(bookingId))
                this.NotFound("Booking not found");

            return this.Ok(_bookings[bookingId]);
        }

        [HttpPost]
        public IActionResult Post(BookingBindingModel model)
        {
            if (model.Nights <= 0)
                return this.BadRequest("Nights must be positive");
            if (!_rentals.ContainsKey(model.RentalId))
                return this.NotFound("Rental not found");

            var rental = _rentals[model.RentalId];
            for (var i = 0; i < model.Nights; i++)
            {
                var count = 0;

                foreach (var booking in _bookings.Values)
                {
                    if (booking.RentalId == model.RentalId
                        && model.End >= booking.Start && model.Start <= booking.End.AddDays(rental.PreparationTimeInDays))
                    {
                        count++;
                    }
                }
                if (count >= rental.Units)
                    return this.Conflict("Not available");
            }


            var key = new ResourceIdViewModel { Id = _bookings.Keys.Count + 1 };


            var bookingCount = _bookings.Values.Count(b => b.RentalId == model.RentalId);

            _bookings.Add(key.Id, new BookingViewModel
            {
                Id = key.Id,
                Nights = model.Nights,
                RentalId = model.RentalId,
                Start = model.Start.Date,
                Unit = (bookingCount + 1) % rental.Units
            });

            return this.Created($"{this.Url.Action("Get", "Bookings")}/{key.Id}", key);
        }
    }
}
