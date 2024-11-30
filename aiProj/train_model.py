import sys
import json
import pandas as pd
from sklearn.model_selection import train_test_split
from sklearn.ensemble import RandomForestClassifier
from sklearn.metrics import accuracy_score

def train_model(model_type, target_column, file_path, params):
    # Читаем параметры
    try:
        params = json.loads(params)
    except json.JSONDecodeError:
        raise ValueError("Params should be a valid JSON string.")

    # Загружаем данные
    data = pd.read_csv(file_path)

    # Разделяем на обучающие и тестовые данные
    X = data.drop(columns=[target_column])
    y = data[target_column]
    X_train, X_test, y_train, y_test = train_test_split(X, y, test_size=0.2, random_state=42)

    # Выбор модели
    if model_type.lower() == "Gradient Boost":
        model = RandomForestClassifier(**params)
    else:
        raise ValueError(f"Model type '{model_type}' is not supported.")

    # Обучение модели
    model.fit(X_train, y_train)

    # Предсказания и оценка
    y_pred = model.predict(X_test)
    accuracy = accuracy_score(y_test, y_pred)

    # Возвращаем результат
    return {"accuracy": accuracy}

if __name__ == "__main__":
    # Получаем аргументы из C#
    model_type = sys.argv[1]
    target_column = sys.argv[2]
    file_path = sys.argv[3]
    params = sys.argv[4]

    # Обучаем модель и получаем метрики
    metrics = train_model(model_type, target_column, file_path, params)

    # Печатаем метрики для передачи обратно в C#
    print(json.dumps(metrics))
