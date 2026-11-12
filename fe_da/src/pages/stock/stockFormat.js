// Number and time formats shared by the stock screens (Vietnam locale and time zone).
export const number = (value) => new Intl.NumberFormat("vi-VN").format(value);
export const dateTime = (value) =>
  value
    ? new Date(value).toLocaleString("vi-VN", { timeZone: "Asia/Ho_Chi_Minh" })
    : "";
