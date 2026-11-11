export const DIMENSION_ANCHORS = {
<<<<<<< HEAD
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
=======
  // ================= BADMINTON (BM-CLB-V1 — Chuẩn BWF & CLB Đánh đôi) =================
  BM_SERVE_NET: {
    subtitle:
      "Độ chuẩn xác giao cầu ngắn/cao sâu, chủ động trả giao cầu và kiểm soát khu vực lưới (chữ T)",
    refereeObservation:
      "Quan sát tối thiểu 8–10 pha giao/trả giao cầu và 5–8 pha xử lý lưới trong trận đôi giao lưu. Đo tỷ lệ cầu đi sát mép lưới (<15cm), điểm rơi sát vạch giao cầu ngắn và khả năng không nâng cầu bổng bị động.",
    subSkills: [
      {
        code: "BM_SN_1",
        name: "Giao cầu ngắn trái tay (Short Backhand Serve)",
        unit: "Số quả giao sát mép lưới (<15cm) & rơi sát vạch / 10 lần",
        tiers: {
          t0: "0–2/10: Thường xuyên rúc lưới, giao phạm luật quá thắt lưng hoặc cầu bổng cao >30cm trên lưới.",
          t1: "3–4/10: Giao qua lưới được nhưng quỹ đạo còn cao, điểm rơi xa vạch tạo đà cho đối thủ vồ cầu.",
          t2: "5–6/10: Giao cầu ngắn vào ô ổn định 5–6/10 quả, độ cao trung bình 15–20cm trên mép lưới.",
          t3: "7–8/10: Giao sát mép lưới (<15cm), điểm rơi chuẩn góc chữ T hoặc biên dọc khiến đối phương phải hất cầu lên.",
          t4: "9–10/10: Nhịp giao kín tay, độ ổn định >90% ngay cả ở điểm số quyết định dưới áp lực lớn.",
        },
      },
      {
        code: "BM_SN_2",
        name: "Giao cầu cao sâu & Búng cầu biến hóa (Flick / High Serve)",
        unit: "Số quả búng cầu qua tầm với đối phương rơi sát vạch cuối / 10 lần",
        tiers: {
          t0: "0–2/10: Chưa biết búng cầu trái tay hoặc giao cao sâu toàn rơi lửng giữa sân.",
          t1: "3–4/10: Có thử búng cầu ra sau nhưng lộ động tác sớm hoặc cầu dễ bay ra ngoài vạch giao đánh đôi.",
          t2: "5–6/10: Búng cầu qua đầu đối phương đứng cao đạt 5–6/10 quả, giữ được điểm rơi trong vạch đôi cuối sân.",
          t3: "7–8/10: Giấu cổ tay cùng một tư thế chuẩn bị cho cả giao ngắn và búng cầu nhanh vào góc trái tay đối thủ.",
          t4: "9–10/10: Biến hóa nhịp độ giao ngắn – búng nhanh cực kỳ hiểm hóc, phá vỡ hoàn toàn thế đứng rình chụp lưới.",
        },
      },
      {
        code: "BM_SN_3",
        name: "Đỡ giao cầu chủ động (Service Return & Push)",
        unit: "Số pha trả giao giành quyền chủ động (đẩy góc / bỏ nhỏ) / 10 lần",
        tiers: {
          t0: "0–2/10: Đứng thụ động chờ cầu rơi thấp rồi hất bổng lên giữa sân hoặc đỡ rúc lưới.",
          t1: "3–4/10: Đỡ giao cầu qua lưới an toàn nhưng chưa biết bước lên đón cầu sớm trên cao.",
          t2: "5–6/10: Biết đặt vợt cao đón cầu sớm, đẩy cầu về hai góc cuối sân hoặc gài lại trên lưới.",
          t3: "7–8/10: Đọc sớm ý đồ giao cầu, chủ động chụp/đẩy cầu nhanh vào người hoặc khoảng trống giữa 2 đối thủ.",
          t4: "9–10/10: Gây áp lực ngay từ pha trả giao đầu tiên, buộc đội giao cầu rơi vào thế phòng thủ bị động.",
        },
      },
      {
        code: "BM_SN_4",
        name: "Bỏ nhỏ, Gài lưới xoáy & Chụp lưới (Net Control & Kill)",
        unit: "Số pha xử lý trên lưới ôm sát băng trắng & dứt điểm gọn / 10 lần",
        tiers: {
          t0: "0–2/10: Bỏ nhỏ nảy cao giữa lưới hoặc chụp lưới toàn vung vợt đập vào mép lưới.",
          t1: "3–4/10: Đưa cầu qua lưới được ở cự ly gần nhưng mặt vợt còn cứng, ít khi dám giữ lại trên lưới.",
          t2: "5–6/10: Bỏ nhỏ thuận/trái tay rơi trong vạch giao cầu ngắn; chụp được các quả cầu lửng cao trên lưới.",
          t3: "7–8/10: Gài cầu chéo lưới (Cross-net) và ve xoáy (Spinning net) sát lưới; bắt bài chụp lưới gọn bằng cổ tay.",
          t4: "9–10/10: Làm chủ tuyệt đối khu vực lưới, giữ cầu thấp ép đối phương phải bung cầu bổng liên tục.",
        },
      },
    ],
    levels: [
      {
        score: 0,
        tag: "Mức 0 — Chưa đạt (<25%)",
        desc: "Giao cầu lỗi/bổng >60%; trả giao và xử lý lưới bị động, thường xuyên tự đánh rúc lưới hoặc hất cầu giữa sân.",
      },
      {
        score: 1,
        tag: "Mức 1 — Cơ bản (30–45%)",
        desc: "Giao cầu qua lưới đạt 4–5/10 quả nhưng còn cao >20cm; đỡ giao cầu an toàn, chưa kiểm soát được độ sát lưới khi bỏ nhỏ.",
      },
      {
        score: 2,
        tag: "Mức 2 — Ổn định (50–65%)",
        desc: "Giao ngắn trái tay ổn định 6/10 quả; biết kết hợp bỏ nhỏ và đẩy cầu ngang lưới trong nhịp giao lưu trung bình.",
      },
      {
        score: 3,
        tag: "Mức 3 — Thuần thục (70–85%)",
        desc: "Giao ngắn sát lưới (<15cm) kết hợp búng cầu giấu tay đạt 7–8/10 quả; chủ động chụp lưới và gài cầu xoáy ép đối phương nâng cầu.",
      },
      {
        score: 4,
        tag: "Mức 4 — Làm chủ (>85%)",
        desc: "Kiểm soát khu vực chữ T và trả giao kín kẽ (>9/10 quả chuẩn); biến hóa cổ tay trên lưới tạo ưu thế tấn công ngay từ 3 chạm đầu.",
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
      },
    ],
  },

<<<<<<< HEAD
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
=======
  BM_CLEAR_BACKHAND: {
    subtitle:
      "Độ sâu đường cầu phông về 2 vạch cuối sân, kỹ thuật ve/bung trái tay và thoát cầu bị động",
    refereeObservation:
      "Quan sát điểm rơi của các pha phông cầu (có tới vùng Rearcourt Back Gallery cách biên cuối <76cm không) và khả năng xử lý khi bị ép sâu vào góc trái tay cuối sân.",
    subSkills: [
      {
        code: "BM_CB_1",
        name: "Phông cầu thuận tay cuối sân (Forehand Deep Clear)",
        unit: "Số quả phông bay tới ô 2 vạch cuối sân (<76cm từ biên) / 10 lần",
        tiers: {
          t0: "0–2/10: Cầu chỉ bay tới 1/2 hoặc 2/3 sân, tạo cơ hội cho đối phương đập cầu dễ dàng.",
          t1: "3–4/10: Phông tới cuối sân khi đứng sẵn thoải mái, nhưng cầu non ngay khi phải vừa lùi vừa đánh.",
          t2: "5–6/10: Phông cao sâu ổn định 6/10 quả về vùng cuối sân, giữ được nhịp thở trong pha cầu bền.",
          t3: "7–8/10: Kết hợp linh hoạt phông cầu bổng phòng thủ (Defensive Clear) và phông lao tấn công (Attacking Clear) vào 2 góc sau.",
          t4: "9–10/10: Phát lực cổ tay gọn nhẹ đưa cầu cắm thẳng vạch cuối sân từ mọi tư thế mà không lộ hướng đánh.",
        },
      },
      {
        code: "BM_CB_2",
        name: "Kỹ thuật Ve cầu trái tay (Backhand Overhead Clear & Drop)",
        unit: "Số pha ve trái tay trên cao qua lưới có kiểm soát điểm rơi / 10 lần",
        tiers: {
          t0: "0–2/10: Lúng túng hoặc đánh hụt nhịp khi cầu sang góc trái tay trên cao.",
          t1: "3–4/10: Đỡ cầu trái tay qua lưới được nhưng cầu chỉ rơi ngay gần lưới hoặc giữa sân.",
          t2: "5–6/10: Biết xoay lưng tỳ ngón cái phát lực ve cầu đi qua 2/3 sân hoặc chặt trái tay bỏ nhỏ qua lưới.",
          t3: "7–8/10: Ve cầu trái tay thẳng và chéo sân thoát sâu về phần sân sau đối phương đạt 7–8/10 quả.",
          t4: "9–10/10: Ve phông sâu hoặc chặt trái tay chéo lưới sắc bén ngay cả khi cầu đã trôi ra sau lưng.",
        },
      },
      {
        code: "BM_CB_3",
        name: "Chặt cầu & Bỏ nhỏ từ cuối sân (Rearcourt Drop & Slice)",
        unit: "Số pha chặt/bỏ nhỏ từ cuối sân rơi trước vạch giao cầu ngắn / 10 lần",
        tiers: {
          t0: "0–2/10: Chưa phân biệt được nhịp phông và chặt cầu; bỏ nhỏ từ cuối sân thường rúc lưới hoặc bay quá dài.",
          t1: "3–4/10: Bỏ nhỏ qua lưới được nhưng tốc độ cầu chậm, bay bổng dễ bị đối phương bắt bài trên lưới.",
          t2: "5–6/10: Chặt cầu thuận tay thẳng và chéo sân rơi quanh vạch giao cầu ngắn đạt 5–6/10 lần.",
          t3: "7–8/10: Cắt cầu nhanh (Fast Drop / Slice) giữ cùng động tác vung vợt với phông/đập, cầu lao nhanh sát lưới.",
          t4: "9–10/10: Điều nhịp phông sâu – chặt chéo cực kỳ chuẩn xác (>85%), kéo giãn hoàn toàn cự ly đội hình đối thủ.",
        },
      },
      {
        code: "BM_CB_4",
        name: "Thoát cầu khi bị ép thế bị động (Passive Rearcourt Recovery)",
        unit: "Số lần cứu cầu sâu 2 góc cuối sân trả về vị trí an toàn / 10 lần",
        tiers: {
          t0: "0–2/10: Khi bị ép sâu hai góc cuối thường bỏ cầu hoặc đánh trả cầu lửng ngay giữa lưới.",
          t1: "3–4/10: Cứu được cầu qua lưới nhưng mất thăng bằng hoàn toàn và bị dứt điểm ở chạm tiếp theo.",
          t2: "5–6/10: Biết dùng cổ tay bung cầu bổng cao về cuối sân hoặc gài bỏ nhỏ để có thời gian hồi vị.",
          t3: "7–8/10: Thoát cầu bị động chéo sân hoặc bung cao sát biên cuối, hóa giải được 7–8/10 pha ép góc.",
          t4: "9–10/10: Phòng thủ góc sau cực kỳ dẻo dai, biến pha cầu bị động thành đường gài cầu phản công bất ngờ.",
        },
      },
    ],
    levels: [
      {
        score: 0,
        tag: "Mức 0 — Cầu non giữa sân (<25%)",
        desc: "Phông cầu chỉ đạt 1/2–2/3 sân; khi bị ép góc trái tay thường đánh hụt nhịp hoặc trả cầu lửng cho đối phương đập.",
      },
      {
        score: 1,
        tag: "Mức 1 — Phông thuận tay cơ bản (30–45%)",
        desc: "Phông thuận tay tới cuối sân khi đứng thoải mái (4/10 quả); góc trái tay mới dừng ở mức đưa cầu qua lưới.",
      },
      {
        score: 2,
        tag: "Mức 2 — Phông sâu & Biết ve trái tay (50–65%)",
        desc: "Phông thuận tay sâu về 2 vạch cuối sân đạt 6/10 quả; biết xoay người ve trái tay và chặt cầu bỏ nhỏ từ cuối sân.",
      },
      {
        score: 3,
        tag: "Mức 3 — Thoát cầu tốt cả 2 góc sau (70–85%)",
        desc: "Phông lao, chặt cầu cắt nhanh và ve trái tay thoát sâu đạt 7–8/10 quả; giữ được độ sâu ngay cả khi phải di chuyển gấp.",
      },
      {
        score: 4,
        tag: "Mức 4 — Điều cầu cuối sân toàn diện (>85%)",
        desc: "Làm chủ hoàn toàn 2 góc cuối sân (>9/10 quả chuẩn), phát lực cổ tay đồng nhất giữa phông – chặt – đập không lộ ý đồ.",
      },
    ],
  },

  BM_SMASH_DEFENSE: {
    subtitle:
      "Uy lực & điểm rơi cú đập cầu, kỹ thuật bung/kê cầu phòng thủ và phản tạt ngang tốc độ cao",
    refereeObservation:
      "Quan sát điểm rơi của các pha đập cầu (vào sát biên dọc, vào khớp vai hay giữa 2 người), khả năng bung cầu cao sâu/kê lưới khi thủ và độ bền không tự đánh hỏng.",
    subSkills: [
      {
        code: "BM_SD_1",
        name: "Đập cầu tấn công & Kiểm soát điểm rơi (Smash Power & Placement)",
        unit: "Số pha đập cầu cắm qua lưới đúng điểm rơi mục tiêu / 10 lần",
        tiers: {
          t0: "0–2/10: Chưa bắt đúng điểm tiếp xúc cầu trên cao; đập cầu thường rúc lưới hoặc bay ngang ra ngoài.",
          t1: "3–4/10: Đập được cầu bổng ở giữa sân qua lưới nhưng quỹ đạo cầu còn bẹt, chưa có độ cắm.",
          t2: "5–6/10: Đập cầu có lực ổn định 6/10 quả, biết nhắm vào khoảng trống giữa hai người hoặc dọc biên.",
          t3: "7–8/10: Kết hợp đập cầu nặng và đập điểm (Half-smash / Stick smash) cắm sát 2 biên dọc với tỷ lệ >75%.",
          t4: "9–10/10: Đập cầu liên hoàn uy lực lớn ở cả tư thế bật nhảy, ép đối phương không thể bung cầu sâu.",
        },
      },
      {
        code: "BM_SD_2",
        name: "Phòng thủ bung cầu & Kê cầu (Defensive Lift & Block)",
        unit: "Số pha đỡ cú đập trả cầu an toàn (cao sâu hoặc kê sát lưới) / 10 lần",
        tiers: {
          t0: "0–2/10: Bị giật mình khi đối phương đập cầu, thường quay mặt hoặc đỡ cầu văng ra ngoài.",
          t1: "3–4/10: Đỡ được các cú đập tốc độ vừa trước mặt nhưng cầu trả lại thường lửng giữa sân.",
          t2: "5–6/10: Biết hạ thấp trọng tâm thủ trái tay, bung cầu cao sâu về cuối sân đạt 5–6/10 lần.",
          t3: "7–8/10: Thủ chắc cả đập thẳng lẫn đập vào người; biết kê nhỏ sát lưới hoặc bung chéo sân đổi hướng tấn công.",
          t4: "9–10/10: Hóa giải >85% các pha đập mạnh, chủ động điều cầu phòng thủ vào góc trống buộc đối phương dừng công.",
        },
      },
      {
        code: "BM_SD_3",
        name: "Phản tạt ngang & Đấu cầu nhanh (Flat Drive & Counter-Attack)",
        unit: "Số pha đôi công tạt ngang giữ cầu đi phẳng sát lưới / 10 lần",
        tiers: {
          t0: "0–2/10: Không theo kịp nhịp cầu đi ngang nhanh, vợt luôn hạ thấp dưới thắt lưng.",
          t1: "3–4/10: Tạt trả được 1–2 nhịp đầu nhưng sau đó mặt vợt bị ngửa làm cầu bổng lên cho đối phương chụp.",
          t2: "5–6/10: Giữ mặt vợt trước ngực, đấu tạt ngang thuận/trái tay ổn định 4–6 chạm liên tục.",
          t3: "7–8/10: Phát lực ngón tay ngắn gọn, tạt cầu đi phẳng sát băng lưới và biết đè cầu chuyển thủ thành công.",
          t4: "9–10/10: Áp đảo trong các pha cầu nhanh ngang lưới, ép nhịp khiến đối phương không kịp thu vợt.",
        },
      },
      {
        code: "BM_SD_4",
        name: "Độ ổn định pha cầu bền (Rally Consistency & Low Unforced Errors)",
        unit: "Tỷ lệ giữ cầu bền 10–15 chạm không tự đánh hỏng / 10 pha cầu",
        tiers: {
          t0: "0–2/10: Tự đánh hỏng (Unforced Error) ngay trong 3 chạm đầu tiên >60% số điểm.",
          t1: "3–4/10: Duy trì được pha cầu 4–6 chạm ở tốc độ chậm, dễ nóng vội đánh hỏng khi kéo dài.",
          t2: "5–6/10: Giữ nhịp cầu 8–12 chạm ổn định trong giao lưu trung bình, lỗi tự hỏng ở mức vừa phải.",
          t3: "7–8/10: Rất ít tự đánh hỏng (<2 lỗi/set ở pha cầu cơ bản), kiên nhẫn chờ đúng quả cầu thuận lợi mới dứt điểm.",
          t4: "9–10/10: Độ ổn định cực cao (>90%) xuyên suốt buổi chơi ngay cả trong các pha đôi công trên 20 chạm.",
        },
      },
    ],
    levels: [
      {
        score: 0,
        tag: "Mức 0 — Công thủ còn lúng túng (<25%)",
        desc: "Chưa bắt nhịp đập cầu trên cao; phản xạ thủ cầu chậm và tỷ lệ tự đánh hỏng sớm trong 3 chạm đầu >60%.",
      },
      {
        score: 1,
        tag: "Mức 1 — Đập & Đỡ đập cơ bản (30–45%)",
        desc: "Đập được cầu bổng giữa sân và đỡ được cú đập lực vừa (4/10 quả), nhưng cầu trả lại còn non và dễ tự hỏng khi tăng tốc.",
      },
      {
        score: 2,
        tag: "Mức 2 — Công thủ ổn định (50–65%)",
        desc: "Đập cầu có hướng kết hợp chặt bỏ nhỏ (6/10 quả); khi thủ biết bung cầu cao sâu hoặc tạt ngang giữ nhịp 8–12 chạm.",
      },
      {
        score: 3,
        tag: "Mức 3 — Đập cắm có điểm rơi & Thủ chắc (70–85%)",
        desc: "Đập cầu cắm sát biên/giữa 2 người; phòng thủ kê lưới và phản tạt chuyển trạng thái từ thủ sang công đạt 7–8/10 pha.",
      },
      {
        score: 4,
        tag: "Mức 4 — Công thủ tốc độ cao toàn diện (>85%)",
        desc: "Duy trì chuỗi tấn công liên hoàn và phản tạt tốc độ cao cực chắc (>9/10 pha chuẩn), tỷ lệ lỗi tự đánh hỏng rất thấp.",
      },
    ],
  },

  BM_FOOTWORK: {
    subtitle:
      "Nhịp nhún chuẩn bị (Split-step), bộ pháp 4 góc sân, giữ thăng bằng và tốc độ hồi vị",
    refereeObservation:
      "Quan sát thời điểm thực hiện nhịp nhún (Split-step) khi đối phương chạm cầu, bước chân thuận khi sải lên lưới/lùi cuối sân (có bị chéo chân không) và thói quen hồi vị.",
    subSkills: [
      {
        code: "BM_FW_1",
        name: "Nhịp nhún chuẩn bị & Phán đoán xuất phát (Split-step Timing)",
        unit: "Số lần thực hiện nhịp nhún đúng thời điểm đối phương chạm cầu / 10 pha",
        tiers: {
          t0: "0–2/10: Đứng thẳng gối chôn chân trên sân, chỉ bắt đầu chạy sau khi cầu đã bay qua lưới.",
          t1: "3–4/10: Có hạ trọng tâm đôi lúc nhưng nhịp nhún còn trễ, dễ bị đứng hình khi đối phương đánh lừa hướng.",
          t2: "5–6/10: Duy trì tư thế sẵn sàng và nhịp nhún cơ bản trong 5–6/10 pha cầu ở tốc độ vừa.",
          t3: "7–8/10: Nhịp nhún Split-step đúng tích tắc đối phương chạm cầu, bứt tốc bước đầu tiên rất nhanh.",
          t4: "9–10/10: Đọc sớm động tác vai/cổ tay đối thủ kết hợp nhịp nhún đàn hồi, luôn đón cầu ở điểm cao nhất.",
        },
      },
      {
        code: "BM_FW_2",
        name: "Bộ pháp tiến lưới 2 góc trước (Front-court Lunge & Balance)",
        unit: "Số lần bước sải chân thuận lên lưới êm, giữ thẳng lưng không chúi người / 10 pha",
        tiers: {
          t0: "0–2/10: Chạy bước nhỏ lắt nhắt hoặc bước chân không thuận lên lưới làm mất đà đổ người về trước.",
          t1: "3–4/10: Biết đưa chân thuận ra trước ở cự ly gần nhưng đầu gối còn vượt quá mũi chân gây chậm nhịp thu về.",
          t2: "5–6/10: Bước Chassé nối sải chân thuận (Lunge) lên 2 góc lưới ổn định 6/10 lần.",
          t3: "7–8/10: Sải chân rộng, tiếp đất bằng gót–bàn chân êm ái, tay không cầm vợt mở rộng giữ thăng bằng chuẩn.",
          t4: "9–10/10: Lao lưới cực nhanh và thu chân bật ngược về vị trí chuẩn bị chỉ trong 1 nhịp.",
        },
      },
      {
        code: "BM_FW_3",
        name: "Bộ pháp lùi 2 góc cuối sân (Rear-court Pivot, Chassé & Scissor Kick)",
        unit: "Số lần xoay hông lùi kịp sau quả cầu trước khi đánh / 10 pha",
        tiers: {
          t0: "0–2/10: Chạy giật lùi mặt nhìn thẳng vào lưới, thường xuyên bị cầu bay qua đầu.",
          t1: "3–4/10: Biết nghiêng người lùi về sau nhưng bước chân còn rối, tiếp xúc cầu khi người đang ngửa ra sau.",
          t2: "5–6/10: Xoay hông di chuyển bước trượt (Chassé) lùi kịp về 2 góc sau trong các đường cầu phông cao.",
          t3: "7–8/10: Thực hiện mượt động tác nhảy đổi chân trên không (Scissor kick / China jump) vừa đánh vừa tạo đà tiến lên.",
          t4: "9–10/10: Bao quát 2 góc cuối sân nhẹ nhàng ít bước, luôn đứng sau quả cầu dù đối phương phông lao nhanh.",
        },
      },
      {
        code: "BM_FW_4",
        name: "Tốc độ Hồi vị & Thể lực duy trì (Base Recovery & Stamina)",
        unit: "Số lần chủ động thu chân hồi vị về vị trí bọc lót ngay sau khi đánh / 10 pha",
        tiers: {
          t0: "0–2/10: Đánh cầu xong đứng nguyên tại góc sân nhìn theo cầu, để trống hoàn toàn phần sân còn lại.",
          t1: "3–4/10: Có ý thức đi bộ quay về giữa sân nhưng nhịp thu chân chậm, hụt hơi sau 1 set đấu.",
          t2: "5–6/10: Hồi vị về khu vực trung tâm đạt 6/10 pha cầu, duy trì được nhịp độ trận giao lưu.",
          t3: "7–8/10: Thu chân hồi vị liền mạch theo hướng đường cầu vừa đánh (nối bước đánh và bước hồi vị thành 1 chuỗi).",
          t4: "9–10/10: Bộ pháp cực kỳ tiết kiệm sức và bền bỉ, giữ nguyên tốc độ di chuyển từ đầu đến cuối buổi chơi.",
        },
      },
    ],
    levels: [
      {
        score: 0,
        tag: "Mức 0 — Di chuyển bản năng (<25%)",
        desc: "Chưa có nhịp nhún chuẩn bị; thường chạy giật lùi hoặc bước chéo chân và đứng nguyên tại góc sân sau khi đánh.",
      },
      {
        score: 1,
        tag: "Mức 1 — Biết bước cơ bản & Hồi vị chậm (30–45%)",
        desc: "Biết bước chân thuận lên lưới và lùi về sau ở tốc độ chậm (4/10 pha); nhịp thu chân hồi vị về giữa sân còn trễ.",
      },
      {
        score: 2,
        tag: "Mức 2 — Bộ pháp 4 góc ổn định (50–65%)",
        desc: "Di chuyển lên lưới, lùi 2 góc sau và hồi vị trung tâm đạt 6/10 pha cầu; giữ được thăng bằng trong các pha cầu bền.",
      },
      {
        score: 3,
        tag: "Mức 3 — Bộ pháp thanh thoát & Có Split-step (70–85%)",
        desc: "Thực hiện nhịp nhún Split-step và nhảy đổi chân (Scissor kick) chuẩn 7–8/10 pha; đổi hướng nhanh khi bị lừa cầu.",
      },
      {
        score: 4,
        tag: "Mức 4 — Bộ pháp chuẩn mực & Bền bỉ (>85%)",
        desc: "Di chuyển ít bước nhưng bao quát toàn sân (>9/10 pha chuẩn), nối liền nhịp đánh và nhịp hồi vị suốt buổi chơi.",
      },
    ],
  },

  BM_DOUBLES: {
    subtitle:
      "Xoay tua đội hình Công (Trên–Dưới) ↔ Thủ (Hai biên), bọc lót trục giữa và tư duy chiến thuật đôi",
    refereeObservation:
      "Quan sát sự chuyển đổi vị trí cùng đồng đội khi cầu bổng (tách ngang 2 bên) so với khi cầu thấp/đập cầu (1 trên lưới – 1 cuối sân), trách nhiệm xử lý cầu trục giữa và lựa chọn chiến thuật.",
    subSkills: [
      {
        code: "BM_DB_1",
        name: "Xoay tua đội hình Công ↔ Thủ (Front-Back to Side-by-Side Rotation)",
        unit: "Số tình huống chuyển đổi đúng đội hình cùng đồng đội / 10 pha",
        tiers: {
          t0: "0–2/10: Đứng cố định 1 bên sân (chia đôi sân trái–phải suốt trận) hoặc chạy chồng lên vị trí đồng đội.",
          t1: "3–4/10: Hiểu khái niệm Công (Trên–Dưới) và Thủ (Hai bên) nhưng chuyển đổi chậm, hay đứng sai khi cầu đổi hướng nhanh.",
          t2: "5–6/10: Biết tràn lên chữ T khi đồng đội lùi đập cầu và tách sang hai bên khi đội nhà bung cầu bổng (6/10 pha).",
          t3: "7–8/10: Xoay tua tròn (Circular rotation) mượt mà: khi người trên lưới phải lùi cứu cầu thì người sau lập tức đảo lên thay vị trí.",
          t4: "9–10/10: Chuyển đổi trạng thái Công–Thủ tức thì như một khối thống nhất với mọi đồng đội ghép cặp.",
        },
      },
      {
        code: "BM_DB_2",
        name: "Bọc lót khoảng trống & Xử lý cầu trục giữa (Middle & Gap Coverage)",
        unit: "Số pha phối hợp kín kẽ ở trục giữa không nhường cầu / va vợt / 10 pha",
        tiers: {
          t0: "0–2/10: Thường xuyên nhường nhau nhìn cầu rơi ở giữa sân hoặc cả hai cùng vung vợt tranh cầu.",
          t1: "3–4/10: Bắt đầu biết nhường cầu cho người thuận tay ở giữa nhưng còn để hở khoảng trống lớn sau lưng người đứng lưới.",
          t2: "5–6/10: Phân định rõ cầu trục giữa thuộc về người có góc thuận tay (Forehand) hoặc người đứng gần cầu hơn.",
          t3: "7–8/10: Người đứng lưới chủ động khép góc chặn đường tạt chéo, người đứng sau bao quát 2 góc cuối rất ăn ý.",
          t4: "9–10/10: Bọc lót kín toàn bộ mặt sân, chủ động lót sau lưng đồng đội ngay khi đồng đội bị kéo lệch biên.",
        },
      },
      {
        code: "BM_DB_3",
        name: "Tư duy chọn giải pháp & Mồi cầu (Tactical Shot Selection & Set-up)",
        unit: "Số quyết định chọn đường cầu hợp lý (không bung cầu bổng vô cớ) / 10 pha",
        tiers: {
          t0: "0–2/10: Cứ chạm cầu là hất bổng lên cao cho đối phương đập xuống vào mặt đồng đội.",
          t1: "3–4/10: Biết giữ cầu qua lại nhưng hay cố đập cầu khi đang ở tư thế xấu làm mất thế chủ động.",
          t2: "5–6/10: Biết chọn bỏ nhỏ/đẩy ngang để giữ quyền tấn công thay vì phông bổng khi không bị ép.",
          t3: "7–8/10: Chủ động điều cầu vào điểm yếu hoặc khoảng trống giữa 2 đối thủ để mồi cầu cho đồng đội tuyến sau dứt điểm.",
          t4: "9–10/10: Đọc trận đấu sắc bén, điều tiết nhịp độ nhanh–chậm linh hoạt giúp cặp đôi luôn nắm quyền kiểm soát.",
        },
      },
      {
        code: "BM_DB_4",
        name: "Giao tiếp trên sân & Thích ứng đồng đội (On-Court Communication)",
        unit: "Mức độ hô gọi cầu ('Của mình/Ngoài') & hỗ trợ tâm lý đồng đội / 10",
        tiers: {
          t0: "0–2/10: Im lặng hoàn toàn trên sân, không báo cầu trong/ngoài hay gọi cầu giữa.",
          t1: "3–4/10: Có giao tiếp cơ bản trước khi giao cầu nhưng ít trao đổi trong khi pha cầu đang diễn ra.",
          t2: "5–6/10: Chủ động hô gọi cầu giữa ('Em!', 'Anh!') và báo cầu ra ngoài biên kịp thời cho đồng đội.",
          t3: "7–8/10: Phối hợp nhịp nhàng với nhiều kiểu đồng đội khác nhau trong buổi chơi Social, biết nhắc vị trí đứng.",
          t4: "9–10/10: Giữ vai trò dẫn dắt thế trận đôi, nâng đỡ đồng đội yếu hơn để duy trì thế trận cân bằng.",
        },
      },
    ],
    levels: [
      {
        score: 0,
        tag: "Mức 0 — Chưa biết đứng đội hình đôi (<25%)",
        desc: "Thường đứng cố định 1 bên sân hoặc di chuyển trùng vị trí đồng đội; hay hất cầu bổng vô cớ làm đồng đội bị đập.",
      },
      {
        score: 1,
        tag: "Mức 1 — Hiểu đội hình Công–Thủ cơ bản (30–45%)",
        desc: "Biết đứng 1 trên – 1 dưới khi công và tách 2 bên khi thủ (4/10 pha), nhưng chuyển đổi còn chậm khi cầu đổi hướng.",
      },
      {
        score: 2,
        tag: "Mức 2 — Xoay tua vị trí nhịp nhàng (50–65%)",
        desc: "Chủ động tràn lưới khi đồng đội lùi đập và tách biên khi bung cầu (6/10 pha); phân chia rõ cầu ở trục giữa sân.",
      },
      {
        score: 3,
        tag: "Mức 3 — Phối hợp mồi cầu & Bọc lót tốt (70–85%)",
        desc: "Xoay tua đảo vị trí mượt mà (7–8/10 pha); biết gài cầu ép điểm yếu đối phương để tạo cơ hội cho đồng đội dứt điểm.",
      },
      {
        score: 4,
        tag: "Mức 4 — Tư duy đánh đôi toàn diện (>85%)",
        desc: "Chuyển đổi Công–Thủ tức thì, bọc lót kín mọi khoảng trống (>9/10 pha) và thích ứng xuất sắc với mọi đồng đội ghép cặp.",
      },
    ],
  },

  // ================= PICKLEBALL (PK-IPTPA-DUPR-V1 — Chuẩn IPTPA & DUPR) =================
  PK_SERVE_RETURN: {
    subtitle:
      "Độ sâu giao bóng & trả giao bóng về 1/3 cuối sân (Baseline) và tốc độ dâng lên vạch bếp (NVZ)",
    refereeObservation:
      "Đếm số quả giao bóng và trả giao bóng rơi sâu vào 1/3 cuối sân đối phương, đồng thời quan sát xem người trả giao có kịp chạy lên đứng vững sát vạch NVZ trước cú đánh thứ 3 hay không.",
    subSkills: [
      {
        code: "PK_SR_1",
        name: "Độ sâu & Ổn định Giao bóng (Deep & Legal Serve)",
        unit: "Số quả giao hợp lệ rơi vào 1/3 cuối sân đối phương / 10 lần",
        tiers: {
          t0: "0–2/10: Giao bóng phạm luật (cao trên rốn) hoặc thường xuyên rúc lưới / bay ra ngoài (>4 lỗi/10 quả).",
          t1: "3–4/10: Giao bóng qua lưới vào đúng ô đạt 6/10 quả nhưng bóng nảy nông ở giữa ô giao bóng.",
          t2: "5–6/10: Giao bóng ổn định >80% vào sân, trong đó 5–6/10 quả rơi sâu về 1/3 cuối sân.",
          t3: "7–8/10: Giao bóng sâu có lực và độ xoáy Topspin (7–8/10 quả), giữ đối phương phải lùi sau vạch cuối sân.",
          t4: "9–10/10: Giao bóng cực sâu sát vạch Baseline kèm biến hóa góc chữ T và góc biên dọc với độ chính xác >90%.",
        },
      },
      {
        code: "PK_SR_2",
        name: "Độ sâu & Độ bổng Trả giao bóng (Deep Return of Serve)",
        unit: "Số pha trả giao sâu về cuối sân giúp có đủ thời gian lên lưới / 10 lần",
        tiers: {
          t0: "0–2/10: Đỡ giao bóng hỏng nhiều hoặc bóng rơi ngay gần lưới cho đội giao bóng lao lên tấn công.",
          t1: "3–4/10: Trả giao bóng qua lưới an toàn nhưng bóng đi ngắn ở giữa sân và tốc độ bay quá nhanh.",
          t2: "5–6/10: Biết đánh trả giao bóng cao sâu về 1/3 cuối sân đạt 5–6/10 lần để kéo dài thời gian bóng bay.",
          t3: "7–8/10: Trả giao bóng sâu, đằm hoặc cắt xoáy ngược (Slice return) vào giữa sân / chân đối thủ đạt 7–8/10 lần.",
          t4: "9–10/10: Trả giao bóng sâu sát Baseline từ cả những quả giao bóng xoáy khó, triệt tiêu hoàn toàn cú Drive thứ 3.",
        },
      },
      {
        code: "PK_SR_3",
        name: "Tốc độ dâng lên vạch NVZ sau Trả giao (Return-and-Approach Timing)",
        unit: "Số lần kịp có mặt và dừng bước (Split-step) sát vạch bếp trước cú thứ 3 / 10 lần",
        tiers: {
          t0: "0–2/10: Sau khi trả giao bóng vẫn đứng nguyên ở cuối sân, quên quy tắc bóng nảy 2 lần (Two-bounce rule).",
          t1: "3–4/10: Có chạy lên nhưng xuất phát trễ, thường bị kẹt lại ở giữa sân (Transition Zone) khi đối phương đánh trả.",
          t2: "5–6/10: Chủ động chạy lên ngay sau khi mặt vợt chạm bóng, kịp tới vạch bếp trong 5–6/10 pha trả giao.",
          t3: "7–8/10: Chạy dâng nhanh và thực hiện nhịp dừng (Split-step) cách vạch bếp <30cm đúng lúc đối phương chạm bóng.",
          t4: "9–10/10: Chuyển trạng thái từ trả giao lên chiếm lĩnh vạch bếp hoàn hảo (>90%), sẵn sàng ép sân ngay từ cú thứ 4.",
        },
      },
      {
        code: "PK_SR_4",
        name: "Điều hướng Giao & Trả giao vào điểm yếu (Target Backhand / Middle)",
        unit: "Số pha chủ động điều bóng vào góc trái tay hoặc chân người di chuyển / 10 lần",
        tiers: {
          t0: "0–2/10: Chỉ cố đánh bóng qua lưới, không có khái niệm nhắm hướng.",
          t1: "3–4/10: Đánh bóng vào đúng ô nhưng thường đưa bóng ngay vào tầm thuận tay của đối thủ.",
          t2: "5–6/10: Bắt đầu biết nhắm trả giao bóng vào góc trái tay (Backhand) của người đứng cuối sân.",
          t3: "7–8/10: Điều bóng chính xác vào góc trái tay sâu hoặc vào trục giữa khiến đối phương khó mở góc đánh.",
          t4: "9–10/10: Khai thác triệt để chân di chuyển và góc yếu của đối phương ngay từ 2 chạm đầu tiên.",
        },
      },
    ],
    levels: [
      {
        score: 0,
        tag: "Mức 0 — Mới làm quen luật & Giao bóng (<25%)",
        desc: "Giao/trả bóng còn lỗi >40%; chưa nắm vững quy tắc bóng nảy 2 lần và thường đứng nguyên ở cuối sân sau khi trả giao.",
      },
      {
        score: 1,
        tag: "Mức 1 — Giao & Trả bóng qua lưới an toàn (30–45%)",
        desc: "Giao và trả bóng vào sân đạt >60% nhưng bóng thường rơi nông ở giữa sân; dâng lên vạch bếp còn chậm.",
      },
      {
        score: 2,
        tag: "Mức 2 — Trả giao sâu & Chủ động lên bếp (50–65%)",
        desc: "Giao/trả bóng sâu về 1/3 cuối sân đạt 5–6/10 quả; sau khi trả giao kịp chạy lên đứng tại vạch bếp đúng nhịp.",
      },
      {
        score: 3,
        tag: "Mức 3 — Giao/Trả xoáy sâu vào góc yếu (70–85%)",
        desc: "Đạt 7–8/10 quả giao/trả sâu vào góc trái tay đối thủ; dừng bước Split-step vững tại vạch NVZ trước cú đánh thứ 3.",
      },
      {
        score: 4,
        tag: "Mức 4 — Áp đặt thế trận từ 2 chạm đầu (>85%)",
        desc: "Giao và trả giao bóng xoáy sát Baseline (>9/10 quả chuẩn), chiếm lĩnh hoàn toàn vạch bếp ngay từ đầu pha bóng.",
      },
    ],
  },

  PK_THIRD_SHOT: {
    subtitle:
      "Kỹ thuật bỏ nhỏ cú thứ 3 (3rd-Shot Drop), đánh căng tầm thấp (Drive) và vượt qua vùng Transition Zone",
    refereeObservation:
      "Quan sát cách đội giao bóng xử lý cú chạm thứ 3 từ cuối sân: có thực hiện được cú Drop rơi êm vào vùng bếp (Unattackable) hoặc phối hợp Drive–Drop để tiến lên vạch NVZ an toàn hay không.",
    subSkills: [
      {
        code: "PK_TS_1",
        name: "Bỏ nhỏ cú thứ 3 vào vùng bếp (3rd-Shot Drop Execution)",
        unit: "Số quả Drop từ cuối sân rơi êm vào vùng NVZ không bị đập trả / 10 lần",
        tiers: {
          t0: "0–2/10: Không biết đánh cú Drop từ cuối sân; bóng hoặc rúc lưới hoặc bay bổng cao cho đối thủ smash.",
          t1: "3–4/10: Đang tập mở mặt vợt nâng bóng nhẹ nhưng 6–7/10 quả vẫn nảy cao trên tầm lưới.",
          t2: "5–6/10: Thực hiện được cú 3rd-Shot Drop thuận tay rơi vào vùng bếp đạt 5–6/10 lần khi bóng trả giao vừa tầm.",
          t3: "7–8/10: Quỹ đạo bóng hình vòng cung chuẩn (Apex bên phần sân nhà), bóng rơi chúi vào vùng NVZ/chân đối phương 7–8/10 quả.",
          t4: "9–10/10: Thực hiện 3rd-Shot Drop êm ái cả thuận tay lẫn trái tay từ sát vạch cuối sân kể cả khi bị trả giao xoáy nặng.",
        },
      },
      {
        code: "PK_TS_2",
        name: "Đánh bóng căng tầm thấp (3rd-Shot Drive & Hybrid Drop)",
        unit: "Số pha Drive đi thấp sát mép lưới tạo bóng bật ra cho cú Drop thứ 5 / 10 lần",
        tiers: {
          t0: "0–2/10: Đánh mạnh hết sức từ cuối sân nhưng bóng toàn bay thẳng ra ngoài biên cuối.",
          t1: "3–4/10: Đánh bóng căng qua lưới được nhưng bóng đi cao ngang ngực đối phương đứng trên lưới.",
          t2: "5–6/10: Kiểm soát lực Drive ~70% kèm xoáy Topspin giúp bóng chúc xuống dưới mép lưới đối phương.",
          t3: "7–8/10: Phối hợp chiến thuật Hybrid (Drive cú thứ 3 vào người đang chạy lên rồi bỏ nhỏ cú thứ 5 để lên lưới).",
          t4: "9–10/10: Giấu động tác giữa Drive và Drop hoàn hảo, khiến đối phương trên lưới không thể bắt bài.",
        },
      },
      {
        code: "PK_TS_3",
        name: "Xử lý bóng ở vùng chuyển tiếp (Transition Zone / 5th-Shot Reset)",
        unit: "Số lần dừng bước làm mềm bóng dưới chân ở vùng giữa sân / 10 pha",
        tiers: {
          t0: "0–2/10: Vừa cắm đầu chạy lên vừa vung vợt đánh bóng ở giữa sân làm bóng văng ra ngoài.",
          t1: "3–4/10: Thường bị chôn chân ở vùng 'No Man's Land' và đỡ bóng bổng lên khi bị đánh vào chân.",
          t2: "5–6/10: Biết dừng bước (Split-step) ở vùng chuyển tiếp khi cú thứ 3 hơi cao để đỡ bóng qua lưới.",
          t3: "7–8/10: Kê mặt vợt làm mềm bóng (Half-volley / Transition Reset) ngay tại chân đưa bóng rơi lại vào vùng bếp.",
          t4: "9–10/10: Tiến lên từng bước cực kỳ điềm tĩnh qua vùng Transition Zone, hóa giải mọi pha tấn công ép chân.",
        },
      },
      {
        code: "PK_TS_4",
        name: "Đọc chất lượng bóng để quyết định Dâng lên hay Giữ vị trí",
        unit: "Số lần ra quyết định tiến lên / đứng thủ đúng theo độ cao quả bóng / 10 pha",
        tiers: {
          t0: "0–2/10: Đánh xong cứ đứng lì ở cuối sân hoặc chạy nhào lên lưới bất chấp quả bóng vừa đánh bị bổng cao.",
          t1: "3–4/10: Đôi lúc nhận ra bóng cao nhưng không kịp dừng đà chạy nên bị đối phương đập thẳng vào người.",
          t2: "5–6/10: Biết chạy lên khi cú Drop tốt (Đèn xanh) và dừng lại thủ khi cú Drop bị cao (Đèn đỏ).",
          t3: "7–8/10: Hai người cùng đọc đúng quỹ đạo bóng để đồng loạt dâng lên chiếm vạch NVZ trong 7–8/10 pha bóng.",
          t4: "9–10/10: Khả năng đọc tình huống bóng chuẩn xác tuyệt đối, chuyển hóa từ thế giao bóng sang cân bằng tại vạch bếp.",
        },
      },
    ],
    levels: [
      {
        score: 0,
        tag: "Mức 0 — Đứng cuối sân đánh bóng (<25%)",
        desc: "Thường đứng lại ở cuối sân đánh qua lại sau khi giao bóng; chưa thực hiện được cú bỏ nhỏ thứ 3 để lên lưới.",
      },
      {
        score: 1,
        tag: "Mức 1 — Tập bỏ nhỏ / Drive lên lưới (30–45%)",
        desc: "Hiểu mục tiêu cần tiến lên vạch bếp sau cú thứ 3; thực hiện được 3–4/10 quả nhưng bóng còn hơi cao hoặc vội chạy khi bóng xấu.",
      },
      {
        score: 2,
        tag: "Mức 2 — Bỏ nhỏ cú thứ 3 vào vùng bếp (50–65%)",
        desc: "Đạt 5–6/10 quả 3rd-Shot Drop rơi vào vùng NVZ hoặc Drive thấp qua lưới, biết dừng bước ở vùng chuyển tiếp để tiến lên.",
      },
      {
        score: 3,
        tag: "Mức 3 — Phối hợp Drive–Drop & Reset tốt (70–85%)",
        desc: "Kết hợp linh hoạt 3rd-Shot Drop và Drive + 5th-Shot Reset đạt 7–8/10 pha; đọc chuẩn thời điểm bóng thấp để cùng đồng đội chiếm bếp.",
      },
      {
        score: 4,
        tag: "Mức 4 — Chuyển trạng thái lên bếp hoàn hảo (>85%)",
        desc: "Đưa bóng rơi sát chân đối phương ở cả thuận/trái tay (>9/10 pha chuẩn), làm mềm bóng mượt mà qua vùng Transition Zone.",
      },
    ],
  },

  PK_DINK_NVZ: {
    subtitle:
      "Kỹ thuật gõ bóng ngắn (Dink), điều hướng chéo sân, độ bền chuỗi Dink và kiểm soát luật vạch bếp",
    refereeObservation:
      "Đếm số chạm duy trì chuỗi Dink liên tục tại vạch NVZ mà không làm bóng nảy cao (Pop-up), khả năng điều bóng chéo sân/ép chân và việc phân biệt bóng có thể tấn công (Attackable) với bóng phải kiên nhẫn.",
    subSkills: [
      {
        code: "PK_DN_1",
        name: "Kiểm soát độ cao & điểm rơi Dink thuận tay (Forehand Dink)",
        unit: "Số quả Dink thuận tay rơi thấp trong vùng NVZ không bị tấn công / 10 lần",
        tiers: {
          t0: "0–2/10: Chạm bóng ở vạch bếp còn cứng tay làm bóng nảy bổng cao quá lưới hoặc rúc lưới.",
          t1: "3–4/10: Gõ bóng ngắn qua lưới được 3–4 nhịp nhưng điểm rơi bóng còn cao, dễ bị đối phương vồ trên không.",
          t2: "5–6/10: Giữ tư thế khuỵu gối thấp, nâng bóng êm từ vai rơi vào vùng bếp đạt 6/10 quả.",
          t3: "7–8/10: Kiểm soát điểm rơi Dink thuận tay sát lưới hoặc đẩy sâu vào góc bếp khiến đối phương phải lùi chân.",
          t4: "9–10/10: Dink thuận tay kèm xoáy Topspin/Slice cực kỳ hiểm hóc (>90%), giữ bóng luôn dưới tầm lưới.",
        },
      },
      {
        code: "PK_DN_2",
        name: "Dink trái tay & Điều bóng chéo sân (Backhand & Cross-Court Dink)",
        unit: "Số pha Dink trái tay / điều bóng đổi hướng chéo sân chuẩn xác / 10 lần",
        tiers: {
          t0: "0–2/10: Khi bóng sang góc trái tay ở vạch bếp thường bị vặn sườn hoặc hất bóng bổng lên.",
          t1: "3–4/10: Đỡ Dink trái tay qua lưới thẳng mặt được nhưng chưa điều được góc chéo sân.",
          t2: "5–6/10: Duy trì được các pha Dink chéo sân (Cross-court) và đỡ bóng trái tay ổn định 5–6/10 quả.",
          t3: "7–8/10: Chủ động đổi hướng bóng từ chéo sân sang thẳng biên (Middle/Line) bằng cả mặt vợt trái tay.",
          t4: "9–10/10: Làm chủ hoàn toàn góc bếp trái tay, bắt Dink trên không (Volley Dink) lấy thời gian của đối thủ.",
        },
      },
      {
        code: "PK_DN_3",
        name: "Độ bền chuỗi Dink & Sự kiên nhẫn (Dink Rally Tolerance)",
        unit: "Khả năng duy trì chuỗi Dink liên tục 8–15 chạm không nóng vội / 10 pha",
        tiers: {
          t0: "0–2/10: Không duy trì nổi quá 2–3 chạm Dink đã tự đánh hỏng hoặc vung vợt đập bừa vào lưới.",
          t1: "3–4/10: Giữ được chuỗi 4–5 chạm nhưng hay mất kiên nhẫn cố tăng tốc khi bóng còn đang ở dưới thấp.",
          t2: "5–6/10: Duy trì chuỗi Dink 6–10 chạm ổn định ở nhịp độ giao lưu mà không tự làm bóng bổng.",
          t3: "7–8/10: Đấu Dink kiên nhẫn 10–15+ chạm, di chuyển bước ngang nhẹ nhàng giữ khoảng cách chuẩn với bóng.",
          t4: "9–10/10: Bản lĩnh đấu Dink bền bỉ tuyệt đối, chủ động gài bẫy buộc đối phương phải mắc lỗi Pop-up trước.",
        },
      },
      {
        code: "PK_DN_4",
        name: "Luật vạch bếp & Chọn đúng bóng tăng tốc (NVZ Footwork & Speed-up)",
        unit: "Số lần xử lý đúng (không phạm lỗi vạch bếp & chọn đúng bóng cao để công) / 10 pha",
        tiers: {
          t0: "0–2/10: Thường xuyên dẫm vạch bếp hoặc bước đà vào bếp khi bắt bóng trên không (NVZ Fault).",
          t1: "3–4/10: Đã nhớ luật không dẫm vạch bếp nhưng đứng quá xa vạch (>60cm) nên dễ bị Dink rơi vào chân.",
          t2: "5–6/10: Đứng sát vạch bếp (<15cm) đúng luật và biết đập bóng khi đối phương Dink bổng cao trên lưới.",
          t3: "7–8/10: Phân biệt rõ bóng không thể công (Unattackable) và bóng hơi cao ngang lưới để tăng tốc cổ tay (Speed-up) vào vai/hông đối thủ.",
          t4: "9–10/10: Kỹ thuật vươn người bắt Volley Dink ngoài vạch bếp và tăng tốc bất ngờ từ những quả bóng tầm trung cực kỳ sắc bén.",
        },
      },
    ],
    levels: [
      {
        score: 0,
        tag: "Mức 0 — Chưa kiểm soát lực chạm bóng ngắn (<25%)",
        desc: "Chạm bóng ở gần lưới còn mạnh làm bóng nảy cao quá lưới; đôi khi còn phạm lỗi bước đà vào vạch bếp khi Volley.",
      },
      {
        score: 1,
        tag: "Mức 1 — Dink cơ bản 3–5 chạm (30–45%)",
        desc: "Nắm rõ luật vạch bếp và gõ bóng ngắn qua lại được 3–5 chạm (4/10 pha), nhưng dễ làm bóng bổng khi bị điều sang trái tay.",
      },
      {
        score: 2,
        tag: "Mức 2 — Duy trì Dink thẳng & chéo sân (50–65%)",
        desc: "Giữ tư thế thấp tại vạch NVZ, duy trì chuỗi Dink 6–10 chạm ổn định (6/10 pha) cả hướng thẳng và chéo sân.",
      },
      {
        score: 3,
        tag: "Mức 3 — Dink kiên nhẫn & Tăng tốc đúng bóng (70–85%)",
        desc: "Đấu Dink bền bỉ 10–15 chạm không nóng vội (7–8/10 pha); biết Dink ép chân và chọn đúng quả bóng cao để Speed-up.",
      },
      {
        score: 4,
        tag: "Mức 4 — Làm chủ hoàn toàn vạch bếp (>85%)",
        desc: "Điều bóng ngắn biến hóa 2 góc kèm xoáy và bắt Volley Dink trên không (>9/10 pha chuẩn), hoàn toàn áp đảo tại vạch NVZ.",
      },
    ],
  },

  PK_VOLLEY_RESET: {
    subtitle:
      "Tư thế vợt sẵn sàng, phản xạ đấu tay đôi (Hand Battle), làm mềm bóng (Reset) và bỏ bóng ngoài",
    refereeObservation:
      "Quan sát vị trí đặt vợt khi đứng tại vạch NVZ, tốc độ phản xạ trong các pha đấu Volley nhanh trên lưới, khả năng lỏng tay hãm lực bóng mạnh rơi lại vào bếp và phán đoán bỏ bóng bay ra ngoài.",
    subSkills: [
      {
        code: "PK_VR_1",
        name: "Tư thế vợt sẵn sàng & Bắt Volley gọn (Paddle-Up & Compact Volley)",
        unit: "Số lần giữ vợt sẵn trước ngực & đẩy bóng gọn không vung vợt rộng / 10 pha",
        tiers: {
          t0: "0–2/10: Luôn buông thõng đầu vợt xuống dưới hông; khi bóng tới thì vung tay quá rộng làm bóng bay bổng.",
          t1: "3–4/10: Có giơ vợt lên nhưng sau khi đánh 1 quả lại quên không thu vợt về vị trí chuẩn bị trước ngực.",
          t2: "5–6/10: Giữ mặt vợt ở vị trí trung tâm (10–11 giờ), thực hiện cú Punch Volley gọn phía trước cơ thể đạt 6/10 lần.",
          t3: "7–8/10: Thu vợt cực nhanh về vị trí chuẩn bị sau mỗi cú chạm, kiểm soát tốt các pha Volley ép xuống chân đối thủ.",
          t4: "9–10/10: Tư thế vợt luôn khóa chặt trước ngực, mở mặt vợt linh hoạt che kín cả vùng vai thuận lẫn vai trái.",
        },
      },
      {
        code: "PK_VR_2",
        name: "Phản xạ đấu tay đôi tốc độ cao (Fast Hand Battle & Counter-Punch)",
        unit: "Số pha thắng thế hoặc giữ vững nhịp trong các loạt đấu Volley nhanh / 10 pha",
        tiers: {
          t0: "0–2/10: Bị giật mình quay người hoặc đỡ trượt ngay khi đối phương tăng tốc bóng trên lưới.",
          t1: "3–4/10: Đỡ lại được cú tăng tốc đầu tiên nhưng mất kiểm soát mặt vợt ở cú chạm thứ hai.",
          t2: "5–6/10: Theo kịp các loạt đấu Volley 3–4 chạm ở tốc độ trung bình mà không bị rối tay.",
          t3: "7–8/10: Phản xạ cổ tay nhanh, biết trượt người (Slide) né bóng đánh vào người để phản đòn (Counter-punch) bằng trái tay.",
          t4: "9–10/10: Cực kỳ nhạy bén trong các pha đấu tay đôi tốc độ cao, phản công cắm thẳng vào chân hoặc góc trống đối phương.",
        },
      },
      {
        code: "PK_VR_3",
        name: "Làm mềm bóng khi bị tấn công mạnh (Block & Reset into NVZ)",
        unit: "Số lần hãm lực cú đập mạnh đưa bóng rơi êm lại vào vùng bếp / 10 pha",
        tiers: {
          t0: "0–2/10: Khi đối phương đập mạnh thì mình cũng đánh mạnh trả lại làm bóng văng xa ra ngoài biên.",
          t1: "3–4/10: Đưa vợt đỡ được bóng mạnh qua lưới nhưng bóng vẫn nảy bổng lên cho đối phương đập tiếp quả thứ 2.",
          t2: "5–6/10: Biết lỏng tay cầm vợt (Soft hands ~3/10 lực nắm) để hãm lực bóng đập trong 5–6/10 lần.",
          t3: "7–8/10: Thực hiện cú Block/Reset từ cả vạch bếp lẫn giữa sân đưa bóng rơi êm vào vùng NVZ, dập tắt đợt tấn công của đối thủ.",
          t4: "9–10/10: Khả năng hấp thụ lực bóng xuất sắc (>85%), biến cú smash của đối phương thành quả bỏ nhỏ sát lưới.",
        },
      },
      {
        code: "PK_VR_4",
        name: "Phán đoán nhả bóng bay ra ngoài (Shoulder-High Out-Ball Judgment)",
        unit: "Số lần phán đoán đúng để né/bỏ các quả Drive mạnh ngang vai bay ra ngoài / 10 pha",
        tiers: {
          t0: "0–2/10: Phản xạ vung vợt đỡ tất cả mọi quả bóng lao tới kể cả bóng cao ngang đầu đang bay thẳng ra ngoài.",
          t1: "3–4/10: Nhận ra bóng đi ngoài quá muộn sau khi đã chạm vợt cứu bóng cho đối phương.",
          t2: "5–6/10: Bắt đầu biết rụt tay bỏ các quả bóng đánh quá mạnh từ cuối sân đi ngang tầm vai.",
          t3: "7–8/10: Đọc sớm lực đánh và góc mặt vợt đối phương để nhả bóng ngoài chuẩn xác đồng thời hô 'Ngoài/Out!' cho đồng đội.",
          t4: "9–10/10: Kỷ luật chọn bóng trên lưới tuyệt vời (>90%), không bao giờ cứu những quả Speed-up quá lực của đối phương.",
        },
      },
    ],
    levels: [
      {
        score: 0,
        tag: "Mức 0 — Phản xạ trên lưới còn chậm (<25%)",
        desc: "Thường hạ thấp vợt và vung tay rộng khi bắt bóng trên không; chưa hãm được lực bóng mạnh và hay đỡ cả bóng ra ngoài.",
      },
      {
        score: 1,
        tag: "Mức 1 — Bắt Volley tốc độ vừa trước mặt (30–45%)",
        desc: "Đỡ được các pha bóng trên không tốc độ trung bình (4/10 pha); khi bị đánh mạnh vào người còn khó làm mềm bóng.",
      },
      {
        score: 2,
        tag: "Mức 2 — Giữ vợt trước ngực & Biết Block bóng (50–65%)",
        desc: "Luôn giơ vợt sẵn trước ngực, bắt Volley gọn và lỏng tay hãm lực bóng mạnh đạt 5–6/10 pha.",
      },
      {
        score: 3,
        tag: "Mức 3 — Đấu tay đôi nhanh & Reset bóng êm (70–85%)",
        desc: "Phản xạ nhanh trong các pha Hand Battle; làm mềm bóng đập mạnh rơi lại vào vùng bếp và biết nhả bóng ngoài (7–8/10 pha).",
      },
      {
        score: 4,
        tag: "Mức 4 — Phản công sắc bén & Phòng thủ vững chắc (>85%)",
        desc: "Đấu tay đôi cực chắc, Reset bóng hiểm hóc từ mọi vị trí và phán đoán bỏ bóng ngoài chuẩn xác (>9/10 pha).",
      },
    ],
  },

  PK_DOUBLES_STRATEGY: {
    subtitle:
      "Di chuyển đồng bộ ngang (Side-by-side Shading), bao trục giữa, chống lốp bóng và chiến thuật đôi",
    refereeObservation:
      "Quan sát khoảng cách 2 người chơi (có giữ sợi dây liên kết 2.5m–3m dịch chuyển cùng nhau theo bóng không), cách phân quyền bóng trục giữa và khả năng bọc lót khi bị Lob qua đầu.",
    subSkills: [
      {
        code: "PK_DS_1",
        name: "Di chuyển ngang đồng bộ cùng đồng đội (Side-by-Side Shading)",
        unit: "Số pha dịch chuyển trái/phải giữ khoảng cách 2.5m–3m khép góc sân / 10 pha",
        tiers: {
          t0: "0–2/10: Thường 1 người đứng trên lưới, 1 người đứng dưới cuối sân tạo khoảng trống chéo sân mênh mông.",
          t1: "3–4/10: Cùng tiến lên vạch bếp nhưng ai đứng nguyên ở nửa sân người đó, không dịch chuyển theo hướng bóng.",
          t2: "5–6/10: Biết đứng ngang hàng tại vạch NVZ và bước theo đồng đội khi bóng bị kéo ra biên (5–6/10 pha).",
          t3: "7–8/10: Dịch chuyển đồng bộ như gắn sợi dây 3m: khi đồng đội ra biên đỡ bóng thì người còn lại lập tức lót vào trục giữa.",
          t4: "9–10/10: Khép kín mọi góc đánh chéo và thẳng biên của đối phương, di chuyển đôi hoàn hảo suốt trận đấu.",
        },
      },
      {
        code: "PK_DS_2",
        name: "Phân quyền & Bao sân trục giữa (Middle Ball & Forehand Priority)",
        unit: "Số pha xử lý bóng trung lộ dứt khoát không nhường nhau / chạm vợt / 10 pha",
        tiers: {
          t0: "0–2/10: Thường xuyên nhìn nhau để bóng đi xuyên qua giữa hai người hoặc va vợt vào nhau.",
          t1: "3–4/10: Còn lưỡng lự khi đối phương đánh bóng vào đúng đường kẻ giữa sân.",
          t2: "5–6/10: Áp dụng quy tắc người có tay thuận (Forehand) ở giữa chủ động nhận các đường bóng trung lộ.",
          t3: "7–8/10: Bọc lót trục giữa rất kín (7–8/10 pha), biết hiểu nguyên tắc 'Ai tạo góc chéo thì người kia giữ trung lộ'.",
          t4: "9–10/10: Chủ động cắt bóng (Poach) trên lưới hợp lý và kiểm soát hoàn toàn khu vực trung lộ.",
        },
      },
      {
        code: "PK_DS_3",
        name: "Xử lý tình huống bị Lốp bóng & Đổi vị trí (Lob Recovery & Switch)",
        unit: "Số pha lùi cứu bóng Lob qua đầu & đổi sân an toàn cùng đồng đội / 10 pha",
        tiers: {
          t0: "0–2/10: Khi bị lốp bóng qua đầu thì chạy lùi thẳng lưng rất nguy hiểm hoặc đứng nhìn bóng rơi.",
          t1: "3–4/10: Có quay người chạy về cứu bóng Lob nhưng hai người không biết đổi vị trí nên để trống một nửa sân.",
          t2: "5–6/10: Biết xoay hông chạy chéo lùi cứu bóng Lob và đánh trả bổng về cuối sân để kịp hồi vị.",
          t3: "7–8/10: Thực hiện chuẩn quy tắc người chéo sân chạy lùi cứu bóng Lob sâu và hô 'Đổi/Switch!' để hoán đổi vị trí.",
          t4: "9–10/10: Hóa giải bóng Lob bằng cú Drop/Reset từ cuối sân và tái thiết lập đội hình trên vạch bếp cực nhanh.",
        },
      },
      {
        code: "PK_DS_4",
        name: "Giao tiếp trên sân & Phối hợp tấn công mục tiêu (Communication & Targeting)",
        unit: "Mức độ hô gọi bóng ('Mine/Yours/Out') & tập trung khai thác điểm yếu đối thủ / 10",
        tiers: {
          t0: "0–2/10: Không giao tiếp với đồng đội, đánh bóng ngẫu nhiên không có ý đồ chiến thuật.",
          t1: "3–4/10: Có gọi bóng đôi lúc nhưng thường tự đánh vào người đang đứng vững hơn bên phía đối phương.",
          t2: "5–6/10: Hô rõ ràng bóng giữa và bóng ra ngoài; biết đánh bóng về phía người còn đang đứng dưới cuối sân.",
          t3: "7–8/10: Phối hợp dồn bóng vào góc trái tay hoặc chân người đang di chuyển để mở khoảng trống dứt điểm.",
          t4: "9–10/10: Tư duy chiến thuật đánh đôi sắc sảo, chỉ huy nhịp độ công–thủ và kết nối ăn ý với mọi đồng đội.",
        },
      },
    ],
    levels: [
      {
        score: 0,
        tag: "Mức 0 — Đội hình đôi rời rạc (<25%)",
        desc: "Thường 1 người đứng trên lưới, 1 người đứng cuối sân; dễ nhường nhau hoặc chạm vợt ở các đường bóng giữa sân.",
      },
      {
        score: 1,
        tag: "Mức 1 — Biết cùng đồng đội tiến lên vạch bếp (30–45%)",
        desc: "Có ý thức dâng lên đứng ngang hàng tại vạch bếp (4/10 pha), nhưng chưa biết dịch chuyển ngang để bọc lót trục giữa.",
      },
      {
        score: 2,
        tag: "Mức 2 — Phân chia trục giữa & Giữ hàng ngang (50–65%)",
        desc: "Thống nhất người có tay thuận ở giữa bao bóng trung lộ (6/10 pha); biết dịch chuyển ngang cơ bản theo hướng bóng.",
      },
      {
        score: 3,
        tag: "Mức 3 — Di chuyển đồng bộ & Chống Lob tốt (70–85%)",
        desc: "Hai người dịch chuyển ngang giữ khoảng cách chuẩn 3m để khép góc (7–8/10 pha); biết đổi vị trí (Switch) khi bị lốp bóng.",
      },
      {
        score: 4,
        tag: "Mức 4 — Chiến thuật đánh đôi hoàn chỉnh (>85%)",
        desc: "Phối hợp cắt bóng (Poach), lùi phòng thủ và dâng tấn công đồng bộ (>9/10 pha chuẩn), khai thác triệt để khoảng trống đối phương.",
      },
    ],
  },

  // ================= LEGACY PILOT CODES (D1..D5 — Tương thích ngược phiếu cũ) =================
  D1: {
    subtitle: "Độ chuẩn xác giao/trả cầu và sự đa dạng các kỹ thuật đánh cầu trên sân",
    refereeObservation:
      "Quan sát giao và trả cầu, phông, nâng, bỏ nhỏ, cầu lưới, tạt và đập cầu; chú ý tỷ lệ kiểm soát thành công trên 10 pha chạm cầu.",
    subSkills: [
      {
        code: "D1_1",
        name: "Giao cầu ngắn & Giao cao sâu chuẩn vạch",
        unit: "Số quả giao cầu chuẩn điểm rơi / 10 lần",
      },
      {
        code: "D1_2",
        name: "Phông cầu cuối sân & Ve trái tay thoát cầu",
        unit: "Số quả phông đạt độ sâu cuối sân / 10 lần",
      },
      {
        code: "D1_3",
        name: "Bỏ nhỏ, Gài lưới & Phản tạt ngang",
        unit: "Số pha kiểm soát cầu sát lưới / 10 lần",
      },
      {
        code: "D1_4",
        name: "Đập cầu tấn công & Chặt cầu điểm rơi",
        unit: "Số pha tấn công có điểm rơi chuẩn / 10 lần",
      },
    ],
    levels: [
      { score: 0, tag: "Mức 0 — Chưa thể hiện (<25%)", desc: "Tỷ lệ thực hiện đúng động tác <25%; thường xuyên rúc lưới hoặc đánh hụt nhịp." },
      { score: 1, tag: "Mức 1 — Bước đầu hình thành (30–45%)", desc: "Đưa cầu qua lưới đạt 4/10 quả khi đứng yên nhưng cầu còn non giữa sân." },
      { score: 2, tag: "Mức 2 — Đáp ứng cơ bản (50–65%)", desc: "Sử dụng ổn định 6/10 pha cầu giao lưu thông thường, còn hạn chế khi bị ép góc." },
      { score: 3, tag: "Mức 3 — Ổn định (70–85%)", desc: "Thực hiện chuẩn xác 7–8/10 pha cầu ở cả thuận tay và trái tay trong nhịp độ nhanh." },
      { score: 4, tag: "Mức 4 — Thích ứng tốt (>85%)", desc: "Kiểm soát toàn diện điểm rơi và biến hóa cổ tay hiệu quả (>9/10 pha chuẩn)." },
    ],
  },
  D2: {
    subtitle: "Nhịp nhún chuẩn bị, bộ pháp di chuyển 4 góc sân và tốc độ hồi vị",
    refereeObservation:
      "Quan sát tư thế sẵn sàng (Split-step), bước chân tới cầu, thăng bằng khi chạm cầu và hồi vị về vị trí trung tâm.",
    subSkills: [
      { code: "D2_1", name: "Nhịp nhún chuẩn bị (Split-step)", unit: "Số lần nhún đúng nhịp / 10 pha" },
      { code: "D2_2", name: "Bộ pháp tiến lưới 2 góc trước", unit: "Số lần sải chân thuận chuẩn / 10 pha" },
      { code: "D2_3", name: "Bộ pháp lùi 2 góc cuối sân", unit: "Số lần xoay hông lùi kịp sau cầu / 10 pha" },
      { code: "D2_4", name: "Tốc độ thu chân hồi vị trung tâm", unit: "Số lần hồi vị kịp thời / 10 pha" },
    ],
    levels: [
      { score: 0, tag: "Mức 0 — Chưa thể hiện (<25%)", desc: "Đứng chôn chân hoặc chạy chéo chân; không hồi vị sau khi chạm cầu." },
      { score: 1, tag: "Mức 1 — Bước đầu hình thành (30–45%)", desc: "Biết bước cơ bản ở tốc độ chậm (4/10 pha) nhưng thu chân hồi vị còn trễ." },
      { score: 2, tag: "Mức 2 — Đáp ứng cơ bản (50–65%)", desc: "Di chuyển đều 4 góc sân và giữ thăng bằng đạt 6/10 pha cầu." },
      { score: 3, tag: "Mức 3 — Ổn định (70–85%)", desc: "Có nhịp nhún Split-step và hồi vị liền mạch đạt 7–8/10 pha cầu." },
      { score: 4, tag: "Mức 4 — Thích ứng tốt (>85%)", desc: "Bộ pháp thanh thoát, bao sân gọn nhẹ và bền bỉ suốt buổi chơi (>9/10 pha)." },
    ],
  },
  D3: {
    subtitle: "Độ ổn định pha cầu bền, kiểm soát điểm rơi và hạn chế lỗi tự đánh hỏng",
    refereeObservation:
      "Quan sát độ dài pha cầu duy trì được (Rally length) và tỷ lệ lỗi tự đánh hỏng (Unforced Errors) qua nhiều pha cầu liên tiếp.",
    subSkills: [
      { code: "D3_1", name: "Duy trì chuỗi cầu bền (8–15 chạm)", unit: "Số pha giữ nhịp cầu bền / 10 lần" },
      { code: "D3_2", name: "Kiểm soát cầu trong vạch biên", unit: "Số quả không đánh ra ngoài / 10 lần" },
      { code: "D3_3", name: "Ổn định khi bị đối phương tăng tốc", unit: "Số pha thủ/trả cầu ổn định / 10 lần" },
      { code: "D3_4", name: "Hạn chế lỗi tự đánh hỏng (Unforced Errors)", unit: "Điểm kiểm soát lỗi tự hỏng / 10" },
    ],
    levels: [
      { score: 0, tag: "Mức 0 — Chưa thể hiện (<25%)", desc: "Tự đánh hỏng ngay trong 3 chạm đầu tiên >60% số pha cầu." },
      { score: 1, tag: "Mức 1 — Bước đầu hình thành (30–45%)", desc: "Giữ cầu được 4–6 chạm ở tốc độ chậm, dễ hỏng khi đổi hướng." },
      { score: 2, tag: "Mức 2 — Đáp ứng cơ bản (50–65%)", desc: "Duy trì pha cầu 8–10 chạm ổn định trong nhịp giao lưu trung bình." },
      { score: 3, tag: "Mức 3 — Ổn định (70–85%)", desc: "Rất ít lỗi tự hỏng (<2 lỗi/set cơ bản), kiểm soát điểm rơi sâu và sát biên." },
      { score: 4, tag: "Mức 4 — Thích ứng tốt (>85%)", desc: "Độ ổn định >90% ngay cả trong các pha cầu tốc độ cao trên 15 chạm." },
    ],
  },
  D4: {
    subtitle: "Tư duy chiến thuật, lựa chọn công–thủ và khai thác khoảng trống",
    refereeObservation:
      "Quan sát quyết định giữa giữ cầu bền, tấn công và phòng thủ; khả năng điều cầu vào khoảng trống và điểm yếu đối phương.",
    subSkills: [
      { code: "D4_1", name: "Chọn giải pháp Công / Thủ đúng tình huống", unit: "Số quyết định hợp lý / 10 pha" },
      { code: "D4_2", name: "Điều cầu khai thác khoảng trống trên sân", unit: "Số pha mở góc hiệu quả / 10 pha" },
      { code: "D4_3", name: "Chuyển trạng thái từ Thủ sang Phản công", unit: "Số pha hóa giải thế bị động / 10 pha" },
      { code: "D4_4", name: "Điều tiết nhịp độ nhanh – chậm", unit: "Điểm bản lĩnh & đọc tình huống / 10" },
    ],
    levels: [
      { score: 0, tag: "Mức 0 — Chưa thể hiện (<25%)", desc: "Đánh cầu theo phản xạ tự nhiên, thường trả cầu vào đúng tầm đập của đối thủ." },
      { score: 1, tag: "Mức 1 — Bước đầu hình thành (30–45%)", desc: "Biết đập cầu khi cầu bổng gần lưới nhưng dễ nóng vội khi cầu ở cuối sân." },
      { score: 2, tag: "Mức 2 — Đáp ứng cơ bản (50–65%)", desc: "Biết phân biệt lúc cần bung cầu thủ và lúc cần bỏ nhỏ/tấn công (6/10 pha)." },
      { score: 3, tag: "Mức 3 — Ổn định (70–85%)", desc: "Chủ động điều cầu vào khoảng trống và ép góc yếu đối thủ đạt 7–8/10 pha." },
      { score: 4, tag: "Mức 4 — Thích ứng tốt (>85%)", desc: "Đọc trận đấu sắc bén, điều chỉnh chiến thuật linh hoạt trước mọi lối đánh." },
    ],
  },
  D5: {
    subtitle: "Xoay tua đội hình đánh đôi, bọc lót trục giữa và giao tiếp cùng đồng đội",
    refereeObservation:
      "Quan sát giao tiếp, trách nhiệm xử lý cầu giữa sân, bọc lót và chuyển đổi đội hình Công (Trên–Dưới) ↔ Thủ (Hai bên).",
    subSkills: [
      { code: "D5_1", name: "Chuyển đổi đội hình Công (Trên-Dưới) ↔ Thủ (Hai bên)", unit: "Số lần xoay tua đúng / 10 pha" },
      { code: "D5_2", name: "Phân chia & Bọc lót cầu trục giữa sân", unit: "Số pha phối hợp kín kẽ / 10 pha" },
      { code: "D5_3", name: "Mồi cầu tạo cơ hội cho đồng đội tuyến sau", unit: "Số pha kiến tạo hiệu quả / 10 pha" },
      { code: "D5_4", name: "Giao tiếp hô gọi cầu & Thích ứng đồng đội", unit: "Điểm phối hợp & giao tiếp / 10" },
    ],
    levels: [
      { score: 0, tag: "Mức 0 — Chưa thể hiện (<25%)", desc: "Đứng cố định 1 bên sân hoặc di chuyển chồng vị trí với đồng đội." },
      { score: 1, tag: "Mức 1 — Bước đầu hình thành (30–45%)", desc: "Hiểu vị trí Công–Thủ cơ bản nhưng xoay tua còn chậm khi cầu đổi hướng." },
      { score: 2, tag: "Mức 2 — Đáp ứng cơ bản (50–65%)", desc: "Xoay tua lên lưới – lùi thủ nhịp nhàng và phân chia rõ cầu trục giữa (6/10 pha)." },
      { score: 3, tag: "Mức 3 — Ổn định (70–85%)", desc: "Bọc lót kín kẽ và biết gài cầu mồi cho đồng đội dứt điểm (7–8/10 pha)." },
      { score: 4, tag: "Mức 4 — Thích ứng tốt (>85%)", desc: "Kết nối ăn ý tức thì với mọi đồng đội ghép cặp, giữ vững cự ly đội hình (>9/10 pha)." },
    ],
  },
};

/**
 * Trả về nhãn phân loại, màu sắc và mức quy đổi cho giá trị thanh trượt 0..10
 */
export function getSliderTierInfo(val10) {
  const v = Number(val10 ?? 0);
  if (v <= 2) {
    return {
      level0to4: 0,
      label: "Chưa đạt / Mới tập",
      bandHint: "B1",
      colorClass: "is-tier-0",
      tierKey: "t0",
    };
  }
  if (v <= 4) {
    return {
      level0to4: 1,
      label: "Cơ bản",
      bandHint: "B2",
      colorClass: "is-tier-1",
      tierKey: "t1",
    };
  }
  if (v <= 6) {
    return {
      level0to4: 2,
      label: "Ổn định",
      bandHint: "B3",
      colorClass: "is-tier-2",
      tierKey: "t2",
    };
  }
  if (v <= 8) {
    return {
      level0to4: 3,
      label: "Thuần thục",
      bandHint: "B4",
      colorClass: "is-tier-3",
      tierKey: "t3",
    };
  }
  return {
    level0to4: 4,
    label: "Làm chủ / Xuất sắc",
    bandHint: "B5",
    colorClass: "is-tier-4",
    tierKey: "t4",
  };
}

export function getSubSkillTierDescription(subSkill, val10) {
  const info = getSliderTierInfo(val10);
  if (subSkill?.tiers && subSkill.tiers[info.tierKey]) {
    return subSkill.tiers[info.tierKey];
  }
  const v = Number(val10 ?? 0);
  if (v <= 2) return `Thực hiện đạt ${v}/10 pha: Còn lỗi kỹ thuật cơ bản, cần hướng dẫn thêm.`;
  if (v <= 4) return `Thực hiện đạt ${v}/10 pha: Đạt mức cơ bản khi đứng thoải mái, còn non khi di chuyển.`;
  if (v <= 6) return `Thực hiện đạt ${v}/10 pha: Duy trì ổn định trong nhịp độ giao lưu trung bình.`;
  if (v <= 8) return `Thực hiện đạt ${v}/10 pha: Kỹ thuật thuần thục, điểm rơi tốt và ít tự đánh hỏng.`;
  return `Thực hiện đạt ${v}/10 pha: Làm chủ kỹ thuật ở tốc độ cao dưới áp lực lớn.`;
}

/**
 * Quy đổi giá trị Mức 0..4 sang giá trị mặc định trên thanh trượt 0..10
 */
export function defaultSliderValueForLevel(score0to4) {
  if (score0to4 == null) return 5;
  const s = Math.round(Number(score0to4));
  if (s <= 0) return 1;
  if (s === 1) return 4;
  if (s === 2) return 6;
  if (s === 3) return 8;
  return 9;
}

/**
 * Tính toán điểm tiêu chí lớn (0..4 và thang 20đ) từ mảng 4 giá trị thanh trượt (0..10)
 */
export function calculateCriterionFromSubSkills(values10) {
  const nums = (values10 || []).map((v) =>
    Math.min(10, Math.max(0, Math.round(Number(v ?? 0))))
  );
  if (nums.length === 0) {
    return { sum40: 0, avg10: 0, exact20: 0, score0to4: 0, weighted20: 0 };
  }
  const sum = nums.reduce((acc, n) => acc + n, 0);
  const maxPossible = nums.length * 10;
  const avg10 = Math.round((sum / nums.length) * 10) / 10;
  const exact20 = Math.round(((sum / maxPossible) * 20) * 10) / 10;
  // Quy đổi chuẩn: 0..4/40 -> Mức 0; 5..14/40 -> Mức 1; 15..24/40 -> Mức 2; 25..34/40 -> Mức 3; 35..40/40 -> Mức 4
  const score0to4 = Math.min(4, Math.max(0, Math.round((sum / maxPossible) * 4)));
  const weighted20 = exact20;
  return {
    sum40: sum,
    avg10,
    exact20,
    score0to4,
    weighted20,
  };
}

/**
 * Đóng gói 4 thanh trượt kỹ năng con + ghi chú thêm của Trọng tài thành chuỗi Evidence chuẩn
 */
export function serializeSubSkillEvidence(criterionCode, subSkillValues, customNote = "") {
  const anchor = DIMENSION_ANCHORS[criterionCode];
  const subSkills = anchor?.subSkills || [];
  if (subSkills.length === 0) {
    return (customNote || "").trim();
  }

  const values = subSkills.map((sk, idx) => {
    const raw = subSkillValues?.[sk.code] ?? subSkillValues?.[idx] ?? 5;
    return Math.min(10, Math.max(0, Math.round(Number(raw))));
  });

  const calc = calculateCriterionFromSubSkills(values);
  const itemsText = subSkills
    .map((sk, idx) => {
      const shortName = sk.name.replace(/\s*\([^)]*\)\s*/g, "").trim();
      return `${shortName}: ${values[idx]}/10`;
    })
    .join(" | ");

  const pairs = subSkills.map((sk, idx) => ({
    name: sk.name.replace(/\s*\([^)]*\)\s*/g, "").trim(),
    val: values[idx],
  }));
  const sortedDesc = [...pairs].sort((a, b) => b.val - a.val);
  const sortedAsc = [...pairs].sort((a, b) => a.val - b.val);

  const best = sortedDesc[0];
  const weakest = sortedAsc[0];

  const analysisParts = [];
  if (best && best.val >= 6) {
    analysisParts.push(`Điểm mạnh: ${best.name} (${best.val}/10)`);
  }
  if (weakest && weakest.val <= 7) {
    analysisParts.push(`Cần cải thiện: ${weakest.name} (${weakest.val}/10)`);
  }

  const headerBlock = `[Chỉ số thành phần: ${itemsText} • TB: ${calc.avg10}/10 (~${calc.exact20}/20đ)]`;
  const analysisText = analysisParts.length > 0 ? `${analysisParts.join(". ")}.` : "";
  const cleanCustom = (customNote || "").trim();

  return [headerBlock, analysisText, cleanCustom ? `Ghi chú sân: ${cleanCustom}` : ""]
    .filter(Boolean)
    .join(" ");
}

/**
 * Giải mã chuỗi Evidence để lấy lại 4 giá trị thanh trượt (0..10), điểm mạnh/yếu và ghi chú riêng
 */
export function parseSubSkillEvidence(criterionCode, evidenceText, fallbackScore0to4 = null) {
  const anchor = DIMENSION_ANCHORS[criterionCode];
  const subSkills = anchor?.subSkills || [];
  const defaultVal = defaultSliderValueForLevel(fallbackScore0to4);

  const defaultMap = {};
  subSkills.forEach((sk) => {
    defaultMap[sk.code] = defaultVal;
  });

  const rawText = String(evidenceText || "").trim();
  if (!rawText) {
    const calc = calculateCriterionFromSubSkills(subSkills.map(() => defaultVal));
    return {
      hasRecordedSliders: false,
      subSkillMap: defaultMap,
      subSkillList: subSkills.map((sk) => ({
        ...sk,
        value10: defaultVal,
        tierInfo: getSliderTierInfo(defaultVal),
        tierDesc: getSubSkillTierDescription(sk, defaultVal),
      })),
      avg10: calc.avg10,
      exact20: calc.exact20,
      strengthText: null,
      weaknessText: null,
      customNote: "",
      rawEvidence: "",
    };
  }

  const blockMatch = rawText.match(/^\[Chỉ số thành phần:\s*([^\]]+)\]\s*(.*)$/s);
  let hasRecordedSliders = false;
  const parsedMap = { ...defaultMap };
  let remainder = rawText;

  if (blockMatch) {
    hasRecordedSliders = true;
    const inner = blockMatch[1].split("•")[0] || "";
    remainder = (blockMatch[2] || "").trim();
    const parts = inner.split("|").map((s) => s.trim());

    parts.forEach((part, idx) => {
      const m = part.match(/:\s*(\d+(?:\.\d+)?)\s*\/\s*10/);
      if (m && subSkills[idx]) {
        parsedMap[subSkills[idx].code] = Math.min(
          10,
          Math.max(0, Math.round(Number(m[1])))
        );
      }
    });
  }

  let strengthText = null;
  let weaknessText = null;
  let customNote = remainder;

  const strengthMatch = customNote.match(/Điểm mạnh:\s*([^.]+\(\d+\/10\))\.\s*/);
  if (strengthMatch) {
    strengthText = strengthMatch[1].trim();
    customNote = customNote.replace(strengthMatch[0], "").trim();
  }

  const weakMatch = customNote.match(/Cần cải thiện:\s*([^.]+\(\d+\/10\))\.\s*/);
  if (weakMatch) {
    weaknessText = weakMatch[1].trim();
    customNote = customNote.replace(weakMatch[0], "").trim();
  }

  customNote = customNote.replace(/^Ghi chú sân:\s*/i, "").trim();

  const valuesList = subSkills.map((sk) => parsedMap[sk.code] ?? defaultVal);
  const calc = calculateCriterionFromSubSkills(valuesList);

  if (!strengthText && subSkills.length > 0) {
    const sortedDesc = subSkills
      .map((sk) => ({
        name: sk.name.replace(/\s*\([^)]*\)\s*/g, "").trim(),
        val: parsedMap[sk.code] ?? defaultVal,
      }))
      .sort((a, b) => b.val - a.val);
    if (sortedDesc[0]?.val >= 6) {
      strengthText = `${sortedDesc[0].name} (${sortedDesc[0].val}/10)`;
    }
  }

  if (!weaknessText && subSkills.length > 0) {
    const sortedAsc = subSkills
      .map((sk) => ({
        name: sk.name.replace(/\s*\([^)]*\)\s*/g, "").trim(),
        val: parsedMap[sk.code] ?? defaultVal,
      }))
      .sort((a, b) => a.val - b.val);
    if (sortedAsc[0]?.val <= 7) {
      weaknessText = `${sortedAsc[0].name} (${sortedAsc[0].val}/10)`;
    }
  }

  return {
    hasRecordedSliders,
    subSkillMap: parsedMap,
    subSkillList: subSkills.map((sk) => {
      const v10 = parsedMap[sk.code] ?? defaultVal;
      return {
        ...sk,
        value10: v10,
        tierInfo: getSliderTierInfo(v10),
        tierDesc: getSubSkillTierDescription(sk, v10),
      };
    }),
    avg10: calc.avg10,
    exact20: calc.exact20,
    strengthText,
    weaknessText,
    customNote,
    rawEvidence: rawText,
  };
}
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
