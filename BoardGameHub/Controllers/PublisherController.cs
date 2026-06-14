using BoardGameHub.Models;
using BoardGameHub.Models.DbModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameHub.Controllers
{
    [Authorize(Roles = "Admin")]

    public class PublisherController : Controller
    {
        private readonly DataBaseContext _context;

        public PublisherController(DataBaseContext context)
        {
            _context = context;
        }

        // Wyświetlanie listy wszystkich wydawców
        [HttpGet]
        public IActionResult ViewAll()
        {
            var publishers = _context.Publishers.ToList();
            return View(publishers);
        }

        // Dodawanie wydawcy (GET)
        [HttpGet]
        public IActionResult Create()
        {
            return View(new Publisher());
        }

        // Dodawanie wydawcy (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Publisher publisher)
        {
            if (ModelState.IsValid)
            {
                _context.Publishers.Add(publisher);
                _context.SaveChanges();
                return RedirectToAction(nameof(ViewAll));
            }
            return View(publisher);
        }

        // Edycja wydawcy (GET)
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var publisher = _context.Publishers.FirstOrDefault(x => x.PublisherId == id);
            if (publisher == null) return NotFound();

            return View(publisher);
        }

        // Edycja wydawcy (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Publisher publisher)
        {
            if (!ModelState.IsValid) return View(publisher);

            _context.Publishers.Update(publisher);
            _context.SaveChanges();
            return RedirectToAction(nameof(ViewAll));
        }

        // 4. Usuwanie wydawcy (GET)
        [HttpGet]
        public IActionResult Delete(int? id)
        {
            if (id == null) return NotFound();

            var publisher = _context.Publishers.FirstOrDefault(x => x.PublisherId == id);
            if (publisher == null) return NotFound();

            var gamesWithPublisher = _context.BoardGames.Where(b => b.PublisherId == id).Count();
            if (gamesWithPublisher > 0)
            {
                TempData["Error"] = $"Nie można usunąć tego wydawcy. Jest używany w {gamesWithPublisher} grze(ach).";
                return RedirectToAction(nameof(ViewAll));
            }

            return View(publisher);
        }

        // 4. Usuwanie wydawcy (POST)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirm(int id)
        {
            var publisher = _context.Publishers.FirstOrDefault(x => x.PublisherId == id);
            if (publisher != null)
            {
                var gamesWithPublisher = _context.BoardGames.Where(b => b.PublisherId == id).Count();
                if (gamesWithPublisher > 0)
                {
                    TempData["Error"] = $"Nie można usunąć tego wydawcy. Jest używany w {gamesWithPublisher} grze(ach).";
                    return RedirectToAction(nameof(ViewAll));
                }

                _context.Publishers.Remove(publisher);
                _context.SaveChanges();
                TempData["Success"] = "Wydawca został pomyślnie usunięty.";
            }
            return RedirectToAction(nameof(ViewAll));
        }
    }
}
