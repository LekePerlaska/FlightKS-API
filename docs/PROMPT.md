# FlightKS — Project Prompts

**29/04/26** - update FlightApp/ to dotnet 10 : Updated TargetFramework to net10.0 and Microsoft.AspNetCore.OpenApi package to 10.0.0 in the csproj, updated CLAUDE.md framework references -

**29/04/26** - add folders for Models -> [Entities, Dtos], Middleware, Helpers and Services -> [Interfaces] : Created the full folder structure under FlightApp/ with .gitkeep files in each leaf directory -

**29/04/26** - create necessary entities for plane flights and users : Created 13 entity classes (User, Airport, Airline, Aircraft, Flight, FlightFare, BaggageOption, Booking, BookingSegment, Passenger, PassengerSegment, PassengerBaggage, Payment) plus Enums.cs under Models/Entities/ -

**29/04/26** - On base folder create docs/ and data/DBContext.cs, data/dbConfig/ : Created docs/PROMPT.md, FlightApp/data/DBContext.cs with AppDbContext covering all 13 DbSets, FlightApp/data/dbConfig/ with .gitkeep, added EF Core 10.0.0 packages to csproj -

**29/04/26** - Add a rule to yourself, every prompt given to you, you are to append it to PROMPT.md : Saved a persistent memory rule and backfilled all prior prompts from this conversation into docs/PROMPT.md -
