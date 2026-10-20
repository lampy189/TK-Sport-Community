// Frontend keys only; map backend Role.Code explicitly when authentication is added.
export const roleHomeConfig = {
  guest: {
    label: "Khách",
    eyebrow: "WELCOME TO TK SPORT",
    title: "Bắt đầu từ một trận đấu.",
    description:
      "Khám phá cầu lông và pickleball. Tìm buổi chơi phù hợp và kết nối với cộng đồng cùng đam mê.",
    action: "Khám phá buổi chơi",
    section: "Khám phá cộng đồng",
    modules: ["Lịch social", "Bộ môn", "Hướng dẫn tham gia"],
    stats: [
      ["02", "Bộ môn"],
      ["01", "Cộng đồng"],
      ["∞", "Cơ hội kết nối"],
    ],
    tasks: [
      "Tìm hiểu cách tham gia social",
      "Khám phá trình độ phù hợp",
      "Chuẩn bị cho buổi chơi đầu tiên",
    ],
  },
  player: {
    label: "Người chơi",
    eyebrow: "YOUR NEXT GAME STARTS HERE",
    title: "Ra sân. Kết nối.\nChơi hết mình.",
    description:
      "Một buổi chơi phù hợp, những người bạn mới. Sẵn sàng cho trải nghiệm thể thao tiếp theo của bạn.",
    action: "Tìm buổi chơi",
    section: "Không gian của bạn",
    modules: ["Lịch social", "Vé của tôi", "Hồ sơ kỹ năng", "Cộng đồng"],
    stats: [
      ["03", "Buổi chơi sắp tới"],
      ["12", "Buổi đã tham gia"],
      ["08", "Bạn cùng chơi"],
    ],
    tasks: [
      "Xem vé và lịch ra sân",
      "Cập nhật hồ sơ kỹ năng",
      "Kết nối bạn cùng chơi",
    ],
  },
  referee: {
    label: "Trọng tài",
    eyebrow: "KEEP THE GAME MOVING",
    title: "Điều phối tốt.\nTrận đấu hay.",
    description:
      "Theo dõi buổi được phân công, ghi nhận điểm danh và đánh giá kỹ năng người chơi.",
    action: "Xem phân công",
    section: "Bàn làm việc trọng tài",
    modules: ["Phân công", "Điểm danh", "Đánh giá kỹ năng", "Vật tư buổi chơi"],
    stats: [
      ["02", "Buổi được phân công"],
      ["16", "Người chơi dự kiến"],
      ["04", "Đánh giá chờ ghi nhận"],
    ],
    tasks: [
      "Kiểm tra danh sách người chơi",
      "Ghi nhận đánh giá tại sân",
      "Theo dõi vật tư được cấp",
    ],
  },
  staff: {
    label: "Nhân viên",
    eyebrow: "READY FOR EVERY SHIFT",
    title: "Vận hành gọn gàng.\nPhục vụ chu đáo.",
    description:
      "Không gian làm việc cho bán hàng tại quầy, kho, kiểm tra thanh toán và đối soát ca.",
    action: "Mở công việc trong ca",
    section: "Công việc trong ca",
    modules: ["Bán hàng", "Kho & vật tư", "Thanh toán", "Đối soát ca"],
    stats: [
      ["08", "Đơn trong ca"],
      ["03", "Thanh toán cần kiểm tra"],
      ["02", "Yêu cầu vật tư"],
    ],
    tasks: [
      "Kiểm tra thanh toán chờ xử lý",
      "Ghi nhận bàn giao hàng hóa",
      "Chuẩn bị đối soát cuối ca",
    ],
  },
  manager: {
    label: "Quản lý",
    eyebrow: "A BETTER DAY ON COURT",
    title: "Bao quát vận hành.\nChủ động mỗi ngày.",
    description:
      "Quản lý lịch social, phân công nhân sự và xử lý yêu cầu trong phạm vi được phê duyệt.",
    action: "Xem lịch vận hành",
    section: "Tổng quan vận hành",
    modules: [
      "Quản lý buổi chơi",
      "Phân công nhân sự",
      "Khiếu nại đánh giá",
      "Ngoại lệ vận hành",
    ],
    stats: [
      ["06", "Buổi chơi hôm nay"],
      ["04", "Nhân sự trong ca"],
      ["03", "Yêu cầu chờ xử lý"],
    ],
    tasks: [
      "Rà soát lịch và phân công",
      "Xem bằng chứng khiếu nại đánh giá",
      "Kiểm tra chênh lệch cần duyệt",
    ],
  },
  owner: {
    label: "Chủ sân",
    eyebrow: "BUILD A STRONGER COMMUNITY",
    title: "Nắm bắt tổng thể.\nĐịnh hướng phát triển.",
    description:
      "Theo dõi hoạt động kinh doanh, chính sách, giá và các phê duyệt thuộc thẩm quyền chủ sân.",
    action: "Xem tổng quan kinh doanh",
    section: "Tổng quan chủ sân",
    modules: [
      "Kinh doanh",
      "Chính sách & giá",
      "Bổ nhiệm quản lý",
      "Phê duyệt",
    ],
    stats: [
      ["06", "Buổi chơi minh họa"],
      ["02", "Chính sách chờ duyệt"],
      ["01", "Đề xuất bổ nhiệm"],
    ],
    tasks: [
      "Rà soát đề xuất chính sách và giá",
      "Xem báo cáo hoạt động",
      "Xử lý yêu cầu vượt thẩm quyền quản lý",
    ],
  },
  "system-admin": {
    label: "Quản trị hệ thống",
    eyebrow: "KEEP THE SYSTEM CONNECTED",
    title: "Hệ thống ổn định.\nKết nối thông suốt.",
    description:
      "Quản lý truy cập đã được phê duyệt, cấu hình kỹ thuật, sự cố xử lý và bàn giao hỗ trợ.",
    action: "Xem trạng thái kỹ thuật",
    section: "Tổng quan kỹ thuật",
    modules: [
      "Tài khoản & truy cập",
      "Cấu hình kỹ thuật",
      "Sự cố",
      "Bàn giao hỗ trợ",
    ],
    stats: [
      ["02", "Yêu cầu cấp truy cập"],
      ["01", "Sự cố đang theo dõi"],
      ["03", "Tích hợp minh họa"],
    ],
    tasks: [
      "Thực hiện quyền truy cập đã duyệt",
      "Kiểm tra sự cố tích hợp",
      "Cập nhật hồ sơ bàn giao kỹ thuật",
    ],
  },
};
