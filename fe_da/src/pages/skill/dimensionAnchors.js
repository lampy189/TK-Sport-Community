export const DIMENSION_ANCHORS = {
  // ================= BADMINTON =================
  BM_SERVE_NET: {
    subtitle:
      "Cảm giác giao cầu, đỡ giao cầu và xử lý các pha cầu ngắn gần lưới",
    levels: [
      {
        score: 0,
        tag: "Mới làm quen",
        desc: "Mới tập giao cầu, đôi khi cầu còn vướng lưới hoặc bay bổng lên giữa sân cho đối phương dễ tấn công.",
      },
      {
        score: 1,
        tag: "Giao cầu qua lưới cơ bản",
        desc: "Giao cầu cao sâu hoặc giao ngắn qua lưới khá đều tay; đỡ giao cầu trả sang sân đối phương an toàn.",
      },
      {
        score: 2,
        tag: "Giao ngắn ổn định & Biết bỏ nhỏ",
        desc: "Giao cầu ngắn trái tay sát mép lưới khá chuẩn; biết bỏ nhỏ và đẩy cầu ngang lưới khi có cơ hội.",
      },
      {
        score: 3,
        tag: "Xử lý lưới linh hoạt",
        desc: "Kết hợp giao cầu ngắn và búng cầu nhanh ra sau để tạo bất ngờ; chủ động chụp lưới và gài cầu xoáy sát lưới.",
      },
      {
        score: 4,
        tag: "Làm chủ khu vực lưới",
        desc: "Giao cầu và trả giao rất kín kẽ, kiểm soát nhịp độ trên lưới và thường xuyên ép đối phương phải nâng cầu bổng.",
      },
    ],
  },
  BM_CLEAR_BACKHAND: {
    subtitle: "Khả năng phông cầu về cuối sân và xử lý khi cầu sang góc trái tay",
    levels: [
      {
        score: 0,
        tag: "Cầu chưa đi hết sân",
        desc: "Phông cầu thường chỉ bay tới khoảng giữa sân; khi cầu sang bên trái tay còn lúng túng hoặc đánh hụt nhịp.",
      },
      {
        score: 1,
        tag: "Phông cầu thuận tay cuối sân",
        desc: "Khi đứng thoải mái bên thuận tay có thể phông cầu sâu về cuối sân; góc trái tay mới dừng ở mức đỡ cầu qua lưới.",
      },
      {
        score: 2,
        tag: "Phông sâu & Biết ve cầu trái tay",
        desc: "Phông cầu thuận tay sâu về cuối sân đều đặn; biết xoay người ve cầu trái tay trả qua lưới về giữa sân hoặc chéo sân.",
      },
      {
        score: 3,
        tag: "Thoát cầu tốt cả hai góc sau",
        desc: "Đánh phông lao hoặc phông phòng thủ đều tới cuối sân; cú trái tay trên cao thoát cầu sâu sang phần sân sau của đối thủ.",
      },
      {
        score: 4,
        tag: "Điều cầu cuối sân toàn diện",
        desc: "Phông cầu và ve trái tay đi thẳng hoặc chéo sân nhẹ nhàng, giữ được độ sâu ngay cả khi đang bị ép thế bị động.",
      },
    ],
  },
  BM_SMASH_DEFENSE: {
    subtitle:
      "Cảm giác đập cầu tấn công, chặt cầu bỏ nhỏ và phản xạ phòng thủ",
    levels: [
      {
        score: 0,
        tag: "Đang tập động tác",
        desc: "Chưa quen nhịp đập cầu trên cao; khi đối phương đập mạnh thường bị giật mình hoặc đỡ cầu văng ra ngoài.",
      },
      {
        score: 1,
        tag: "Đập cầu & Đỡ đập cơ bản",
        desc: "Đập được các quả cầu bổng ở giữa sân qua lưới; đỡ được những cú đập vừa phải trả lại qua lưới.",
      },
      {
        score: 2,
        tag: "Biết phối hợp Đập & Chặt cầu",
        desc: "Đập cầu có hướng xuống hai biên kết hợp chặt cầu bỏ nhỏ; khi phòng thủ biết bung cầu cao sâu hoặc tạt ngang.",
      },
      {
        score: 3,
        tag: "Tấn công có điểm rơi & Thủ chắc",
        desc: "Đập cầu cắm và có lực tốt; khi phòng thủ biết điều cầu vào khoảng trống để chuyển nhanh từ thủ sang phản công.",
      },
      {
        score: 4,
        tag: "Công thủ tốc độ cao",
        desc: "Duy trì chuỗi tấn công đập cầu rồi tràn lưới dứt điểm liên tục; phòng thủ phản tạt rất chắc trước các pha cầu nhanh.",
      },
    ],
  },
  BM_FOOTWORK: {
    subtitle: "Nhịp bước chân di chuyển khắp sân và giữ thăng bằng sau khi đánh",
    levels: [
      {
        score: 0,
        tag: "Di chuyển theo bản năng",
        desc: "Chạy theo cầu tự nhiên, đôi khi còn bước chéo chân hoặc đứng nguyên tại chỗ sau khi vừa chạm cầu.",
      },
      {
        score: 1,
        tag: "Có ý thức lùi về giữa sân",
        desc: "Biết bước chân thuận lên lưới và lùi về sau cơ bản; sau khi đánh xong có thói quen quay lại khu vực trung tâm.",
      },
      {
        score: 2,
        tag: "Di chuyển đều các góc sân",
        desc: "Bộ pháp lên lưới, lùi hai góc sau khá nhịp nhàng, giữ được thăng bằng cơ thể trong các pha cầu qua lại.",
      },
      {
        score: 3,
        tag: "Bước chân thanh thoát & Linh hoạt",
        desc: "Có nhịp nhún chuẩn bị trước khi đối phương chạm cầu, đổi hướng nhanh khi bị lừa cầu và hồi vị rất kịp thời.",
      },
      {
        score: 4,
        tag: "Bộ pháp gọn & Bền bỉ",
        desc: "Di chuyển ít bước nhưng bao quát toàn sân, bật nhảy và thu chân liền mạch, giữ được tốc độ tốt suốt buổi chơi.",
      },
    ],
  },
  BM_DOUBLES: {
    subtitle: "Sự ăn ý khi đứng sân đánh đôi và khả năng giữ nhịp pha cầu bền",
    levels: [
      {
        score: 0,
        tag: "Chưa quen cách đứng đội hình đôi",
        desc: "Thường đứng cố định một bên sân hoặc di chuyển trùng vào vị trí của đồng đội; pha cầu thường kết thúc sớm.",
      },
      {
        score: 1,
        tag: "Hiểu vị trí Công - Thủ cơ bản",
        desc: "Biết đứng một trên một dưới khi đội nhà tấn công và tách sang hai bên khi phông cầu bổng để phòng thủ.",
      },
      {
        score: 2,
        tag: "Xoay tua vị trí nhịp nhàng",
        desc: "Chủ động tràn lên bắt lưới khi đồng đội lùi đập cầu và lùi xuống bọc lót khi đồng đội tiến lên; giữ cầu bền ít tự hỏng.",
      },
      {
        score: 3,
        tag: "Phối hợp tạo cơ hội cho đồng đội",
        desc: "Biết điều cầu ép điểm yếu của đối phương để mồi cầu cho đồng đội tuyến sau dứt điểm.",
      },
      {
        score: 4,
        tag: "Bọc lót & Đọc tình huống ăn ý",
        desc: "Chuyển đổi trạng thái công – thủ tức thì cùng đồng đội, hầu như không để lộ khoảng trống trên sân.",
      },
    ],
  },

  // ================= PICKLEBALL =================
  PK_SERVE_RETURN: {
    subtitle: "Cảm giác giao bóng, trả giao bóng sâu và di chuyển lên vạch bếp",
    levels: [
      {
        score: 0,
        tag: "Đang làm quen luật & Giao bóng",
        desc: "Đang làm quen cách giao bóng dưới thắt lưng và quy tắc bóng nảy hai lần; đôi khi giao bóng còn ra ngoài hoặc chạm lưới.",
      },
      {
        score: 1,
        tag: "Giao & Trả bóng qua lưới an toàn",
        desc: "Giao bóng vào ô khá đều tay; đỡ giao bóng trả qua lưới ổn định nhưng bóng thường rơi ở khoảng giữa sân.",
      },
      {
        score: 2,
        tag: "Trả giao sâu & Chủ động tiến lên",
        desc: "Giao bóng và trả giao bóng sâu về cuối sân đối phương, sau khi trả bóng kịp chạy dâng lên sát vạch bếp.",
      },
      {
        score: 3,
        tag: "Giao bóng có hướng & Lên lưới vững",
        desc: "Biết giao bóng sâu vào góc trái tay đối thủ; trả giao bóng sâu để có đủ thời gian chạy lên đứng vững ở vạch bếp.",
      },
      {
        score: 4,
        tag: "Giao & Trả bóng xoáy áp đặt",
        desc: "Giao và trả giao bóng có độ xoáy, điểm rơi sát vạch cuối sân giúp cặp đấu chiếm lợi thế trên lưới ngay từ đầu.",
      },
    ],
  },
  PK_THIRD_SHOT: {
    subtitle:
      "Cách xử lý cú đánh thứ 3 từ cuối sân để tìm đường tiến lên vạch bếp",
    levels: [
      {
        score: 0,
        tag: "Thường đứng cuối sân đánh bóng",
        desc: "Sau khi giao bóng thường đứng lại ở vạch cuối sân đánh qua lại, bóng dễ bay cao cho đối phương trên lưới đập xuống.",
      },
      {
        score: 1,
        tag: "Tập bỏ nhỏ hoặc đánh thẳng lên lưới",
        desc: "Hiểu mục tiêu cần tiến lên vạch bếp sau cú đánh thứ 3; đang tập bỏ nhỏ từ cuối sân nhưng bóng đôi khi còn hơi cao.",
      },
      {
        score: 2,
        tag: "Bỏ nhỏ cú thứ 3 vào vùng bếp",
        desc: "Thực hiện được cú bỏ nhỏ rơi êm vào vùng bếp hoặc đánh bóng thấp qua lưới để cùng đồng đội tiến dần lên trên.",
      },
      {
        score: 3,
        tag: "Kết hợp đánh nhanh & Bỏ nhỏ linh hoạt",
        desc: "Biết chọn lúc đánh bóng căng tầm thấp rồi nối tiếp bằng cú bỏ nhỏ rơi sát lưới để dâng lên vạch bếp an toàn.",
      },
      {
        score: 4,
        tag: "Lên lưới mượt mà từ mọi vị trí",
        desc: "Đưa bóng rơi thấp vào chân đối phương ở cả bên thuận tay lẫn trái tay, tiến lên chiếm vạch bếp rất tự nhiên.",
      },
    ],
  },
  PK_DINK_NVZ: {
    subtitle: "Khả năng gõ bóng ngắn (Dink) và kiểm soát bóng tại vạch bếp",
    levels: [
      {
        score: 0,
        tag: "Chưa quen lực chạm bóng ngắn",
        desc: "Khi đứng gần lưới chạm bóng còn hơi mạnh làm bóng nảy cao; đôi khi bước đà theo bóng chạm vào vạch bếp.",
      },
      {
        score: 1,
        tag: "Gõ bóng qua lại vài nhịp cơ bản",
        desc: "Nắm rõ quy tắc không dẫm vạch bếp khi bắt bóng trên không; giữ được vài nhịp bóng ngắn trước khi bóng nảy cao.",
      },
      {
        score: 2,
        tag: "Duy trì nhịp Dink thẳng & chéo sân",
        desc: "Giữ tư thế thấp tại vạch bếp, gõ bóng ngắn qua lại ổn định và biết điều bóng chéo sân.",
      },
      {
        score: 3,
        tag: "Dink kiên nhẫn & Biết chọn bóng tấn công",
        desc: "Đấu bóng ngắn bền bỉ không nóng vội; biết gõ bóng ép chân đối phương và nhận ra bóng hơi cao để tăng tốc độ.",
      },
      {
        score: 4,
        tag: "Kiểm soát hoàn toàn vạch bếp",
        desc: "Điều bóng ngắn biến hóa hai góc kèm độ xoáy, chủ động gài đối phương nâng bóng cao để dứt điểm.",
      },
    ],
  },
  PK_VOLLEY_RESET: {
    subtitle:
      "Phản xạ bắt bóng trên không (Volley) và làm mềm bóng khi bị tấn công",
    levels: [
      {
        score: 0,
        tag: "Phản xạ trên lưới còn chậm",
        desc: "Khi bóng đi nhanh trên lưới thường vung vợt rộng hoặc đỡ bóng bay bổng ra ngoài sân.",
      },
      {
        score: 1,
        tag: "Bắt bóng tốc độ vừa trước mặt",
        desc: "Đỡ được các pha bóng trên không ở tốc độ trung bình; khi đối phương đánh mạnh vào người còn khó hãm lực bóng.",
      },
      {
        score: 2,
        tag: "Bắt Volley gọn & Biết giảm lực bóng",
        desc: "Luôn giơ vợt sẵn trước ngực, đẩy bóng gọn gàng và bắt đầu biết lỏng tay hãm lực khi đứng ở vùng giữa sân.",
      },
      {
        score: 3,
        tag: "Đấu tay đôi nhanh & Reset bóng êm",
        desc: "Phản xạ nhanh trong các pha đôi công tốc độ cao trên lưới; làm mềm được những cú đánh mạnh rơi êm lại vào vùng bếp.",
      },
      {
        score: 4,
        tag: "Phản công sắc bén & Biết bỏ bóng ngoài",
        desc: "Đấu tay đôi rất chắc, phản đòn chuẩn xác, đồng thời phán đoán tốt để nhả những quả bóng mạnh bay ra ngoài.",
      },
    ],
  },
  PK_DOUBLES_STRATEGY: {
    subtitle: "Sự phối hợp di chuyển cùng đồng đội và bao sân khi đánh đôi",
    levels: [
      {
        score: 0,
        tag: "Di chuyển còn lệch nhịp đồng đội",
        desc: "Thường một người đứng trên lưới còn một người đứng dưới cuối sân; dễ nhường nhau hoặc chạm vợt ở bóng giữa sân.",
      },
      {
        score: 1,
        tag: "Biết cùng đồng đội tiến lên vạch bếp",
        desc: "Có ý thức dâng lên đứng ngang hàng cùng đồng đội tại vạch bếp; đánh bóng nảy đất bên thuận tay ổn định.",
      },
      {
        score: 2,
        tag: "Phân chia bóng ở trục giữa sân",
        desc: "Đánh bóng cuối sân đều cả hai bên; biết thống nhất người có tay thuận ở giữa sẽ chủ động bao các đường bóng trung lộ.",
      },
      {
        score: 3,
        tag: "Di chuyển ngang đồng bộ cùng nhau",
        desc: "Hai người dịch chuyển sang trái/phải nhịp nhàng theo hướng bóng để khép góc, biết lốp bóng qua đầu khi cần.",
      },
      {
        score: 4,
        tag: "Phối hợp chiến thuật đôi ăn ý",
        desc: "Hiểu ý nhau trong việc đổi vị trí, cắt bóng trên lưới và cùng lùi phòng thủ hoặc cùng dâng tấn công rất nhịp nhàng.",
      },
    ],
  },
};
