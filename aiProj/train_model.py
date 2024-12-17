import pandas as pd
import numpy as np
import json
import sys
from sklearn.model_selection import train_test_split
from sklearn.metrics import accuracy_score, mean_squared_error, f1_score, r2_score
from sklearn.ensemble import RandomForestClassifier, RandomForestRegressor
from sklearn.linear_model import LogisticRegression, LinearRegression
from sklearn.svm import SVC, SVR

def load_data(filepath):
    """
    Загружает данные из CSV файла.
    """
    try:
        data = pd.read_csv(filepath)
    except FileNotFoundError:
        raise ValueError(f"File '{filepath}' not found.")
    return data

def get_model(algorithm, params):
    """
    Возвращает модель в зависимости от названия алгоритма и применяет переданные параметры.
    """
    if algorithm == 'RandomForestClassifier':
        return RandomForestClassifier(**params)
    elif algorithm == 'LogisticRegression':
        return LogisticRegression(**params)
    elif algorithm == 'SVC':
        return SVC(**params)
    elif algorithm == 'RandomForestRegressor':
        return RandomForestRegressor(**params)
    elif algorithm == 'LinearRegression':
        return LinearRegression(**params)
    elif algorithm == 'SVR':
        return SVR(**params)
    else:
        raise ValueError(f"Unsupported algorithm: {algorithm}")

def main():
    # Получение аргументов командной строки
    if len(sys.argv) < 4:
        raise ValueError("Usage: python script.py <algorithm> <target_column> <filepath> <paramsJson>")

    algorithm = sys.argv[1]
    target_column = sys.argv[2]
    filepath = sys.argv[3]
    params_json = sys.argv[4] if len(sys.argv) > 4 else "{}"
    params_json = params_json.encode('utf-8').decode('unicode_escape')

    try:
        params = json.loads(params_json)
    except json.JSONDecodeError as e:
        sys.exit(1)

    # Загружаем данные
    data = load_data(filepath)

    if target_column not in data.columns:
        raise ValueError(f"Target column '{target_column}' not found in data")

    # Обработка пропущенных значений
    data = data.fillna(data.mean(numeric_only=True))

    # Разделяем данные на признаки и целевую переменную
    X = data.drop(columns=[target_column])
    y = data[target_column]

    # Разделяем данные на обучающую и тестовую выборки
    X_train, X_test, y_train, y_test = train_test_split(X, y, test_size=0.2, random_state=42)


    # Получаем модель с параметрами
    model = get_model(algorithm, params)

    # Обучаем модель
    model.fit(X_train, y_train)

    # Делаем прогнозы
    y_pred = model.predict(X_test)

    # Составляем результаты метрик
    results = {}

    if np.issubdtype(y_test.dtype, np.integer):
        results['Accuracy'] = accuracy_score(y_test, y_pred)
        results['F1 Score'] = f1_score(y_test, y_pred, average='weighted')
    else:
        results['Mean Squared Error'] = mean_squared_error(y_test, y_pred)
        results['R2 Score'] = r2_score(y_test, y_pred)

    # Выводим результаты в формате JSON
    print(json.dumps(results))

if __name__ == "__main__":
    main()
