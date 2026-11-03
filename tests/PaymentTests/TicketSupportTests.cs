using TKSC.Application.Services;
using TKSC.Application.DTOs;
using Xunit;
public class TicketSupportTests
{
 [Fact] public void AcceptsPngQrImage()
 {
  var png="iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jRZkAAAAASUVORK5CYII=";
  var image=TicketSupportService.ValidateImage(png);
  Assert.Equal("image/png",image.Type);
  Assert.Equal(Convert.FromBase64String(png),image.Bytes);
 }
 [Theory]
 [InlineData("not base64")]
 [InlineData("PHN2Zz48L3N2Zz4=")]
 [InlineData("aGVsbG8=")]
 public void RejectsInvalidOrNonRasterAttachments(string content)=>Assert.Throws<PaymentException>(()=>TicketSupportService.ValidateImage(content));
 [Fact] public void RejectsOversizedAttachment()=>Assert.Throws<PaymentException>(()=>TicketSupportService.ValidateImage(Convert.ToBase64String(new byte[2097153])));
 [Fact] public void TextOnlyMessageNeedsNoImage()=>Assert.Null(TicketSupportService.ValidateImage(null).Bytes);
}
