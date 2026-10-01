using OfficeBite.Mobile.Helpers;
using OfficeBite.Mobile.Models;

namespace OfficeBite.Mobile.Helpers;

public class AggregatedFoodItem
{
    public int MenuItemId { get; set; }
    public string FoodName { get; set; } = string.Empty;
    public int TotalQuantity { get; set; }
    public int EmployeeCount { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalAmount => UnitPrice * TotalQuantity;
    public string TotalDisplay => $"Rs.{TotalAmount:0}";
    public string PricePerUnitDisplay => UnitPrice > 0 ? $"@ Rs.{UnitPrice:0}" : string.Empty;
    public string EmployeeSummary { get; set; } = string.Empty;
    public List<string> EmployeeList { get; set; } = new();
}

public static class PdfReportHelper
{
    public static async Task<string> GenerateOrderSummaryPdfAsync(
        string dateFormatted,
        TodaySummaryDto? summary,
        List<OrderDto> orders,
        List<AggregatedFoodItem> foodItems)
    {
#if ANDROID
        return await Task.Run(() =>
        {
            var document = new Android.Graphics.Pdf.PdfDocument();
            int pageWidth = 595;
            int pageHeight = 842;
            int margin = 36;
            int usableWidth = pageWidth - (margin * 2);
            int pageNumber = 1;

            var pageInfo = new Android.Graphics.Pdf.PdfDocument.PageInfo.Builder(pageWidth, pageHeight, pageNumber).Create();
            var page = document.StartPage(pageInfo);
            var canvas = page.Canvas;

            var paint = new Android.Graphics.Paint { AntiAlias = true };
            float y = 0;

            void CheckPageBreak(float neededHeight)
            {
                if (y + neededHeight > pageHeight - 50)
                {
                    DrawFooter(canvas, paint, pageWidth, pageHeight, margin, pageNumber);
                    document.FinishPage(page);

                    pageNumber++;
                    var newPageInfo = new Android.Graphics.Pdf.PdfDocument.PageInfo.Builder(pageWidth, pageHeight, pageNumber).Create();
                    page = document.StartPage(newPageInfo);
                    canvas = page.Canvas;
                    y = 40;

                    DrawSubHeader(canvas, paint, pageWidth, margin, dateFormatted, pageNumber);
                    y += 35;
                }
            }

            // --- HEADER BANNER ---
            paint.Color = Android.Graphics.Color.ParseColor("#6D28D9");
            paint.SetStyle(Android.Graphics.Paint.Style.Fill);
            canvas.DrawRect(0, 0, pageWidth, 85, paint);

            paint.Color = Android.Graphics.Color.White;
            paint.TextSize = 20;
            paint.SetTypeface(Android.Graphics.Typeface.Create(Android.Graphics.Typeface.Default, Android.Graphics.TypefaceStyle.Bold));
            canvas.DrawText("OfficeBite - Food Order Summary", margin, 38, paint);

            paint.Color = Android.Graphics.Color.ParseColor("#DDD6FE");
            paint.TextSize = 11;
            paint.SetTypeface(Android.Graphics.Typeface.Default);
            canvas.DrawText($"Date: {dateFormatted}  |  Generated: {DateTimeHelper.NowIst:dd MMM yyyy, hh:mm tt}", margin, 62, paint);

            y = 105;

            // --- STATS OVERVIEW ---
            int totalOrders = summary?.TotalOrders ?? orders.Count;
            int totalItemsToCook = foodItems.Sum(f => f.TotalQuantity);
            decimal totalRevenue = summary?.TotalAmount ?? orders.Where(o => o.Status != "Cancelled").Sum(o => o.TotalAmount);
            int pendingCount = summary?.PendingOrders ?? orders.Count(o => o.Status == "Pending");
            int confirmedCount = summary?.ConfirmedOrders ?? orders.Count(o => o.Status == "Confirmed");
            int deliveredCount = summary?.DeliveredOrders ?? orders.Count(o => o.Status == "Delivered" || o.Status == "Completed");

            paint.Color = Android.Graphics.Color.ParseColor("#F8FAFC");
            paint.SetStyle(Android.Graphics.Paint.Style.Fill);
            var statsRect = new Android.Graphics.RectF(margin, y, pageWidth - margin, y + 55);
            canvas.DrawRoundRect(statsRect, 10, 10, paint);

            paint.Color = Android.Graphics.Color.ParseColor("#CBD5E1");
            paint.SetStyle(Android.Graphics.Paint.Style.Stroke);
            paint.StrokeWidth = 1;
            canvas.DrawRoundRect(statsRect, 10, 10, paint);

            float colWidth = usableWidth / 4f;
            DrawStatItem(canvas, paint, margin + 10, y + 20, "TOTAL ORDERS", totalOrders.ToString(), "#0F172A");
            DrawStatItem(canvas, paint, margin + colWidth + 5, y + 20, "ITEMS TO COOK", totalItemsToCook.ToString(), "#7C3AED");
            DrawStatItem(canvas, paint, margin + (colWidth * 2) + 5, y + 20, "TOTAL REVENUE", $"Rs.{totalRevenue:0}", "#059669");
            DrawStatItem(canvas, paint, margin + (colWidth * 3) + 5, y + 20, "STATUS", $"{confirmedCount} Conf | {pendingCount} Pend", "#2563EB");

            y += 75;

            // --- SECTION 1: FOOD PREPARATION / COOKING QUANTITIES ---
            paint.SetStyle(Android.Graphics.Paint.Style.Fill);
            paint.Color = Android.Graphics.Color.ParseColor("#0F172A");
            paint.TextSize = 13;
            paint.SetTypeface(Android.Graphics.Typeface.Create(Android.Graphics.Typeface.Default, Android.Graphics.TypefaceStyle.Bold));
            canvas.DrawText("FOOD ITEMS PREPARATION SUMMARY (COOKING QUANTITIES)", margin, y, paint);
            y += 10;

            // Food Table Header
            float tblHeaderHeight = 24;
            paint.Color = Android.Graphics.Color.ParseColor("#7C3AED");
            canvas.DrawRect(margin, y, pageWidth - margin, y + tblHeaderHeight, paint);

            paint.Color = Android.Graphics.Color.White;
            paint.TextSize = 9;
            paint.SetTypeface(Android.Graphics.Typeface.Create(Android.Graphics.Typeface.Default, Android.Graphics.TypefaceStyle.Bold));
            canvas.DrawText("SN", margin + 8, y + 16, paint);
            canvas.DrawText("FOOD ITEM NAME", margin + 35, y + 16, paint);
            canvas.DrawText("QTY ORDERED", margin + 230, y + 16, paint);
            canvas.DrawText("EMPLOYEES ORDERED BY", margin + 330, y + 16, paint);
            y += tblHeaderHeight;

            int itemIdx = 1;
            if (foodItems.Count == 0)
            {
                paint.Color = Android.Graphics.Color.ParseColor("#94A3B8");
                paint.TextSize = 10;
                paint.SetTypeface(Android.Graphics.Typeface.Default);
                canvas.DrawText("No food orders placed yet today.", margin + 15, y + 20, paint);
                y += 30;
            }
            else
            {
                foreach (var item in foodItems)
                {
                    CheckPageBreak(28);

                    // Row bg
                    paint.Color = (itemIdx % 2 == 0)
                        ? Android.Graphics.Color.ParseColor("#F8FAFC")
                        : Android.Graphics.Color.White;
                    paint.SetStyle(Android.Graphics.Paint.Style.Fill);
                    canvas.DrawRect(margin, y, pageWidth - margin, y + 25, paint);

                    // Border line
                    paint.Color = Android.Graphics.Color.ParseColor("#E2E8F0");
                    paint.SetStyle(Android.Graphics.Paint.Style.Stroke);
                    paint.StrokeWidth = 0.5f;
                    canvas.DrawLine(margin, y + 25, pageWidth - margin, y + 25, paint);

                    // Text
                    paint.SetStyle(Android.Graphics.Paint.Style.Fill);
                    paint.Color = Android.Graphics.Color.ParseColor("#475569");
                    paint.TextSize = 9;
                    paint.SetTypeface(Android.Graphics.Typeface.Default);
                    canvas.DrawText(itemIdx.ToString(), margin + 8, y + 16, paint);

                    // Food Name (Bold)
                    paint.Color = Android.Graphics.Color.ParseColor("#0F172A");
                    paint.SetTypeface(Android.Graphics.Typeface.Create(Android.Graphics.Typeface.Default, Android.Graphics.TypefaceStyle.Bold));
                    canvas.DrawText(item.FoodName, margin + 35, y + 16, paint);

                    // Quantity Badge
                    paint.Color = Android.Graphics.Color.ParseColor("#EDE9FE");
                    var badgeRect = new Android.Graphics.RectF(margin + 230, y + 4, margin + 300, y + 21);
                    canvas.DrawRoundRect(badgeRect, 5, 5, paint);

                    paint.Color = Android.Graphics.Color.ParseColor("#6D28D9");
                    paint.SetTypeface(Android.Graphics.Typeface.Create(Android.Graphics.Typeface.Default, Android.Graphics.TypefaceStyle.Bold));
                    paint.TextSize = 10;
                    canvas.DrawText($"x {item.TotalQuantity} Qty", margin + 238, y + 16, paint);

                    // Employee summary text
                    paint.Color = Android.Graphics.Color.ParseColor("#475569");
                    paint.SetTypeface(Android.Graphics.Typeface.Default);
                    paint.TextSize = 9;
                    string empSummary = item.EmployeeSummary;
                    if (empSummary.Length > 35) empSummary = empSummary.Substring(0, 32) + "...";
                    canvas.DrawText(empSummary, margin + 330, y + 16, paint);

                    y += 25;
                    itemIdx++;
                }

                // Total Row
                CheckPageBreak(26);
                paint.Color = Android.Graphics.Color.ParseColor("#EDE9FE");
                paint.SetStyle(Android.Graphics.Paint.Style.Fill);
                canvas.DrawRect(margin, y, pageWidth - margin, y + 22, paint);

                paint.Color = Android.Graphics.Color.ParseColor("#6D28D9");
                paint.TextSize = 10;
                paint.SetTypeface(Android.Graphics.Typeface.Create(Android.Graphics.Typeface.Default, Android.Graphics.TypefaceStyle.Bold));
                canvas.DrawText("TOTAL FOOD ITEMS TO COOK:", margin + 35, y + 15, paint);
                canvas.DrawText($"{totalItemsToCook} Total Items", margin + 230, y + 15, paint);
                y += 32;
            }

            // --- SECTION 2: EMPLOYEE ORDERS DETAIL ---
            CheckPageBreak(50);
            paint.SetStyle(Android.Graphics.Paint.Style.Fill);
            paint.Color = Android.Graphics.Color.ParseColor("#0F172A");
            paint.TextSize = 13;
            paint.SetTypeface(Android.Graphics.Typeface.Create(Android.Graphics.Typeface.Default, Android.Graphics.TypefaceStyle.Bold));
            canvas.DrawText("EMPLOYEE ORDERS BREAKDOWN", margin, y, paint);
            y += 10;

            // Orders Table Header
            paint.Color = Android.Graphics.Color.ParseColor("#334155");
            canvas.DrawRect(margin, y, pageWidth - margin, y + tblHeaderHeight, paint);

            paint.Color = Android.Graphics.Color.White;
            paint.TextSize = 9;
            paint.SetTypeface(Android.Graphics.Typeface.Create(Android.Graphics.Typeface.Default, Android.Graphics.TypefaceStyle.Bold));
            canvas.DrawText("#", margin + 8, y + 16, paint);
            canvas.DrawText("EMPLOYEE NAME", margin + 28, y + 16, paint);
            canvas.DrawText("ITEMS ORDERED", margin + 155, y + 16, paint);
            canvas.DrawText("STATUS", margin + 380, y + 16, paint);
            canvas.DrawText("TOTAL", margin + 465, y + 16, paint);
            y += tblHeaderHeight;

            int empIdx = 1;
            if (orders.Count == 0)
            {
                paint.Color = Android.Graphics.Color.ParseColor("#94A3B8");
                paint.TextSize = 10;
                paint.SetTypeface(Android.Graphics.Typeface.Default);
                canvas.DrawText("No employee orders placed yet.", margin + 15, y + 20, paint);
                y += 30;
            }
            else
            {
                foreach (var ord in orders)
                {
                    CheckPageBreak(28);

                    paint.Color = (empIdx % 2 == 0)
                        ? Android.Graphics.Color.ParseColor("#F8FAFC")
                        : Android.Graphics.Color.White;
                    paint.SetStyle(Android.Graphics.Paint.Style.Fill);
                    canvas.DrawRect(margin, y, pageWidth - margin, y + 25, paint);

                    paint.Color = Android.Graphics.Color.ParseColor("#E2E8F0");
                    paint.SetStyle(Android.Graphics.Paint.Style.Stroke);
                    paint.StrokeWidth = 0.5f;
                    canvas.DrawLine(margin, y + 25, pageWidth - margin, y + 25, paint);

                    paint.SetStyle(Android.Graphics.Paint.Style.Fill);
                    paint.Color = Android.Graphics.Color.ParseColor("#475569");
                    paint.TextSize = 9;
                    paint.SetTypeface(Android.Graphics.Typeface.Default);
                    canvas.DrawText(empIdx.ToString(), margin + 8, y + 16, paint);

                    paint.Color = Android.Graphics.Color.ParseColor("#0F172A");
                    paint.SetTypeface(Android.Graphics.Typeface.Create(Android.Graphics.Typeface.Default, Android.Graphics.TypefaceStyle.Bold));
                    string uName = ord.UserName;
                    if (uName.Length > 18) uName = uName.Substring(0, 16) + "..";
                    canvas.DrawText(uName, margin + 28, y + 16, paint);

                    string itemsStr = string.Join(", ", ord.Items.Select(i => $"{i.FoodName} x{i.Quantity}"));
                    if (!string.IsNullOrWhiteSpace(ord.SpecialInstructions))
                        itemsStr += $" (Note: {ord.SpecialInstructions})";
                    if (itemsStr.Length > 40) itemsStr = itemsStr.Substring(0, 38) + "...";

                    paint.Color = Android.Graphics.Color.ParseColor("#334155");
                    paint.SetTypeface(Android.Graphics.Typeface.Default);
                    paint.TextSize = 9;
                    canvas.DrawText(itemsStr, margin + 155, y + 16, paint);

                    string statusColorHex = ord.Status switch
                    {
                        "Confirmed" => "#2563EB",
                        "Delivered" => "#059669",
                        "Completed" => "#059669",
                        "Cancelled" => "#DC2626",
                        _ => "#D97706"
                    };
                    paint.Color = Android.Graphics.Color.ParseColor(statusColorHex);
                    paint.SetTypeface(Android.Graphics.Typeface.Create(Android.Graphics.Typeface.Default, Android.Graphics.TypefaceStyle.Bold));
                    canvas.DrawText(ord.Status, margin + 380, y + 16, paint);

                    paint.Color = Android.Graphics.Color.ParseColor("#0F172A");
                    canvas.DrawText(ord.TotalDisplay, margin + 465, y + 16, paint);

                    y += 25;
                    empIdx++;
                }
            }

            DrawFooter(canvas, paint, pageWidth, pageHeight, margin, pageNumber);
            document.FinishPage(page);

            string fileName = $"OfficeBite_Order_Summary_{DateTimeHelper.NowIst:yyyyMMdd_HHmmss}.pdf";
            string filePath = Path.Combine(FileSystem.CacheDirectory, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                document.WriteTo(stream);
            }
            document.Close();

            return filePath;
        });
#else
        await Task.Delay(100);
        return string.Empty;
#endif
    }

#if ANDROID
    private static void DrawStatItem(Android.Graphics.Canvas canvas, Android.Graphics.Paint paint, float x, float y, string label, string value, string valueColorHex)
    {
        paint.SetStyle(Android.Graphics.Paint.Style.Fill);
        paint.Color = Android.Graphics.Color.ParseColor("#64748B");
        paint.TextSize = 8;
        paint.SetTypeface(Android.Graphics.Typeface.Create(Android.Graphics.Typeface.Default, Android.Graphics.TypefaceStyle.Bold));
        canvas.DrawText(label, x, y, paint);

        paint.Color = Android.Graphics.Color.ParseColor(valueColorHex);
        paint.TextSize = 13;
        paint.SetTypeface(Android.Graphics.Typeface.Create(Android.Graphics.Typeface.Default, Android.Graphics.TypefaceStyle.Bold));
        canvas.DrawText(value, x, y + 16, paint);
    }

    private static void DrawSubHeader(Android.Graphics.Canvas canvas, Android.Graphics.Paint paint, int pageWidth, int margin, string date, int pageNum)
    {
        paint.SetStyle(Android.Graphics.Paint.Style.Fill);
        paint.Color = Android.Graphics.Color.ParseColor("#6D28D9");
        canvas.DrawRect(0, 0, pageWidth, 28, paint);

        paint.Color = Android.Graphics.Color.White;
        paint.TextSize = 10;
        paint.SetTypeface(Android.Graphics.Typeface.Create(Android.Graphics.Typeface.Default, Android.Graphics.TypefaceStyle.Bold));
        canvas.DrawText($"OfficeBite Order Summary • {date} (Cont.)", margin, 18, paint);
    }

    private static void DrawFooter(Android.Graphics.Canvas canvas, Android.Graphics.Paint paint, int pageWidth, int pageHeight, int margin, int pageNum)
    {
        paint.SetStyle(Android.Graphics.Paint.Style.Stroke);
        paint.Color = Android.Graphics.Color.ParseColor("#CBD5E1");
        paint.StrokeWidth = 0.8f;
        canvas.DrawLine(margin, pageHeight - 30, pageWidth - margin, pageHeight - 30, paint);

        paint.SetStyle(Android.Graphics.Paint.Style.Fill);
        paint.Color = Android.Graphics.Color.ParseColor("#94A3B8");
        paint.TextSize = 8;
        paint.SetTypeface(Android.Graphics.Typeface.Default);
        canvas.DrawText("OfficeBite Lunch Ordering System • Confidential & Internal Use", margin, pageHeight - 16, paint);
        canvas.DrawText($"Page {pageNum}", pageWidth - margin - 35, pageHeight - 16, paint);
    }
#endif
}
