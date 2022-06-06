using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using VacationRental.Api.Models;

namespace VacationRental.Api.Controllers
{
    [Route("api/v1/rentals")]
    [ApiController]
    public class RentalsController : ControllerBase
    {
        private readonly IDictionary<int, RentalViewModel> _rentals;
        private readonly IDictionary<int, BookingViewModel> _booking;

        public RentalsController(
            IDictionary<int, RentalViewModel> rentals,
            IDictionary<int, BookingViewModel> bookings)
        {
            _rentals = rentals;
            _booking = bookings;
        }

        [HttpGet]
        [Route("{rentalId:int}")]
        public IActionResult Get(int rentalId)
        {
            if (!_rentals.ContainsKey(rentalId))
                return this.NotFound("Rental not found");

            return this.Ok(_rentals[rentalId]);
        }

        [HttpPost]
        public IActionResult Post(RentalBindingModel model)
        {
            var key = new ResourceIdViewModel { Id = _rentals.Keys.Count + 1 };

            _rentals.Add(key.Id, new RentalViewModel
            {
                Id = key.Id,
                Units = model.Units,
                PreparationTimeInDays = model.PreparationTimeInDays
            });

            return this.Created($"{this.Url.Action("Get", "Rentals")}/{key.Id}", key);
        }

        [HttpPut]
        [Route("{rentalId:int}")]
        public IActionResult Put(int rentalId, RentalBindingModel model)
        {
            if (!_rentals.ContainsKey(rentalId))
            {
                return this.NotFound("Rental not found.");
            }

            _rentals[rentalId].Units = model.Units;
            _rentals[rentalId].PreparationTimeInDays = model.PreparationTimeInDays;

            var existingBookings = _booking.Values.Where(b => b.RentalId == rentalId).OrderBy(b => b.Start).ToArray();

            for (var i = 0; i < existingBookings.Length; i++)
            {
                // Adding PreparationTimeInDays overlaps into the next booking
                if (existingBookings[i].End.AddDays(model.PreparationTimeInDays) >= existingBookings[i + 1].Start)
                {
                    return this.Conflict("Extending preparation days is overlapping with existing bookings");
                }
            }

            return this.Ok(_rentals[rentalId]);
        }
    }
}
