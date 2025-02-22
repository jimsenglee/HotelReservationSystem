using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System.IO;

namespace HotelRoomReservationSystem.BLL
{
    public class PdfGenerator
    {
        public byte[] GenerateInvoice(Models.Transaction transaction)
        {
            using (var stream = new MemoryStream())
            {
                PdfDocument document = new PdfDocument();
                PdfPage page = document.AddPage();
                XGraphics gfx = XGraphics.FromPdfPage(page);

                // Define fonts
                XFont titleFont = new XFont("Times New Roman", 24, XFontStyleEx.Bold);
                XFont headerFont = new XFont("Times New Roman", 14, XFontStyleEx.Bold);
                XFont regularFont = new XFont("Times New Roman", 12, XFontStyleEx.Regular);

                string userName = transaction.Users?.Name ?? transaction.Reservation.UserName;
                string userEmail = transaction.Users?.Email ?? transaction.Reservation.UserEmail;

                if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(userEmail))
                {
                    throw new Exception("User name or email is missing for the invoice.");
                }

                // Margins
                double margin = 40;
                double yPoint = margin;

                // Add title
                gfx.DrawString("INVOICE", titleFont, XBrushes.Black, new XRect(0, yPoint, page.Width, page.Height), XStringFormats.TopCenter);
                yPoint += 40;

                // Company Information
                gfx.DrawString("Hotel Reservation System", headerFont, XBrushes.Black, new XRect(margin, yPoint, page.Width - 2 * margin, page.Height), XStringFormats.TopLeft);
                yPoint += 20;
                gfx.DrawString("Address Line 1: 77, Lorong Lembah Permai 3, 11200 Tanjung Bungah, Pulau Pinang", regularFont, XBrushes.Black, new XRect(margin, yPoint, page.Width - 2 * margin, page.Height), XStringFormats.TopLeft);
                yPoint += 15;
                gfx.DrawString("Phone: 04 - 666 9999", regularFont, XBrushes.Black, new XRect(margin, yPoint, page.Width - 2 * margin, page.Height), XStringFormats.TopLeft);
                yPoint += 30;

                // Customer Information
                gfx.DrawString("Bill To:", headerFont, XBrushes.Black, new XRect(margin, yPoint, page.Width - 2 * margin, page.Height), XStringFormats.TopLeft);
                yPoint += 20;
                gfx.DrawString($"Name: {userName}", regularFont, XBrushes.Black, new XRect(margin, yPoint, page.Width - 2 * margin, page.Height), XStringFormats.TopLeft);
                yPoint += 15;
                gfx.DrawString($"Email: {userEmail}", regularFont, XBrushes.Black, new XRect(margin, yPoint, page.Width - 2 * margin, page.Height), XStringFormats.TopLeft);
                yPoint += 30;

                // Transaction Details
                gfx.DrawString($"Transaction Number: {transaction.Id}", regularFont, XBrushes.Black, new XRect(margin, yPoint, page.Width - 2 * margin, page.Height), XStringFormats.TopLeft);
                yPoint += 15;
                gfx.DrawString($"Invoice Date: {DateTime.Now.ToShortDateString()}", regularFont, XBrushes.Black, new XRect(margin, yPoint, page.Width - 2 * margin, page.Height), XStringFormats.TopLeft);
                yPoint += 15;
                gfx.DrawString($"Payment Method: {transaction.PaymentMethod}", regularFont, XBrushes.Black, new XRect(margin, yPoint, page.Width - 2 * margin, page.Height), XStringFormats.TopLeft);
                yPoint += 30;

                // Reservation Details
                gfx.DrawString($"Check-In Date: {transaction.Reservation.CheckInDate:MM/dd/yyyy}", regularFont, XBrushes.Black, new XRect(margin, yPoint, page.Width - 2 * margin, page.Height), XStringFormats.TopLeft);
                yPoint += 15;
                gfx.DrawString($"Check-Out Date: {transaction.Reservation.CheckOutDate:MM/dd/yyyy}", regularFont, XBrushes.Black, new XRect(margin, yPoint, page.Width - 2 * margin, page.Height), XStringFormats.TopLeft);
                yPoint += 30;

                // Table Headers
                gfx.DrawString("Description", headerFont, XBrushes.Black, new XRect(margin, yPoint, 300, page.Height), XStringFormats.TopLeft);
                gfx.DrawString("Amount (RM)", headerFont, XBrushes.Black, new XRect(page.Width - margin - 100, yPoint, 100, page.Height), XStringFormats.TopRight);
                yPoint += 20;

                // Calculate amounts
                decimal totalPrice = transaction.Amount;
                decimal roomBasePrice = totalPrice / 1.06m; // Exclude 6% SST
                decimal sstTax = totalPrice - roomBasePrice; // SST amount

                // Table Content - Room
                gfx.DrawString($"Room: {transaction.Reservation.Room.RoomType.Name}", regularFont, XBrushes.Black, new XRect(margin, yPoint, 300, page.Height), XStringFormats.TopLeft);
                gfx.DrawString($"{roomBasePrice.ToString("N2")}", regularFont, XBrushes.Black, new XRect(page.Width - margin - 100, yPoint, 100, page.Height), XStringFormats.TopRight);
                yPoint += 20;

                // Table Content - SST
                gfx.DrawString("6% SST", regularFont, XBrushes.Black, new XRect(margin, yPoint, 300, page.Height), XStringFormats.TopLeft);
                gfx.DrawString($"{sstTax.ToString("N2")}", regularFont, XBrushes.Black, new XRect(page.Width - margin - 100, yPoint, 100, page.Height), XStringFormats.TopRight);
                yPoint += 20;

                // Total Amount
                gfx.DrawLine(XPens.Black, margin, yPoint, page.Width - margin, yPoint);
                yPoint += 10;
                gfx.DrawString("Total", headerFont, XBrushes.Black, new XRect(margin, yPoint, 300, page.Height), XStringFormats.TopLeft);
                gfx.DrawString($"{totalPrice.ToString("N2")}", headerFont, XBrushes.Black, new XRect(page.Width - margin - 100, yPoint, 100, page.Height), XStringFormats.TopRight);
                yPoint += 30;

                // Footer
                gfx.DrawString("Thank you for your reservation!", regularFont, XBrushes.Black, new XRect(0, page.Height - margin, page.Width, page.Height), XStringFormats.BottomCenter);

                document.Save(stream, false);
                return stream.ToArray();
            }
        }
    }
}
